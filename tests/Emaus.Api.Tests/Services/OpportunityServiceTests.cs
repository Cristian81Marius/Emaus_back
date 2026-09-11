using Emaus.Api.Dtos.Opportunities;
using Emaus.Api.Services.Notifications;
using Emaus.Api.Services.Opportunities;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Entities;

namespace Emaus.Api.Tests.Services;

public class OpportunityServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly OpportunityService _sut;
    private readonly ApplicationUser _volunteer1;
    private readonly ApplicationUser _volunteer2;

    public OpportunityServiceTests()
    {
        _volunteer1 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "David", Phone = "0711111111", PasswordHash = "x" };
        _volunteer2 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Ligia", Phone = "0722222222", PasswordHash = "x" };
        _testDb.Db.Users.AddRange(_volunteer1, _volunteer2);
        _testDb.Db.SaveChanges();

        var notificationService = new NotificationService(
            _testDb.Repo<ApplicationUser>(), _testDb.Repo<Notification>(), _testDb.Repo<UserPushToken>(),
            _testDb.UnitOfWork(), new NullPushNotificationSender());
        _sut = new OpportunityService(_testDb.Repo<VolunteerOpportunity>(), _testDb.Repo<OpportunitySignup>(), _testDb.UnitOfWork(), notificationService);
    }

    public void Dispose() => _testDb.Dispose();

    [Fact]
    public async Task GetAllAsync_NoScheduledDate_SortsLast()
    {
        await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Event, "Fara data", null, null, false, null, null, false), _volunteer1.Id);
        await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Cleaning, "Cu data", null, new DateTime(2026, 12, 1), true, null, null, false), _volunteer1.Id);

        var all = await _sut.GetAllAsync(_volunteer1.Id);

        Assert.Equal("Cu data", all[0].Title);
        Assert.Equal("Fara data", all[1].Title);
    }

    [Fact]
    public async Task CreateAsync_NotifyEveryoneTrue_NotifiesAllVolunteers()
    {
        await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Event, "Titlu", null, null, false, null, null, NotifyEveryone: true), _volunteer1.Id);

        var recipients = _testDb.Db.Notifications.Select(n => n.RecipientUserId).ToList();
        Assert.Contains(_volunteer1.Id, recipients);
        Assert.Contains(_volunteer2.Id, recipients);
    }

    [Fact]
    public async Task CreateAsync_NotifyEveryoneFalse_SendsNoNotification()
    {
        await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Event, "Titlu", null, null, false, null, null, NotifyEveryone: false), _volunteer1.Id);

        Assert.Empty(_testDb.Db.Notifications);
    }

    [Fact]
    public async Task SignUpAsync_Idempotent_DoesNotDuplicate()
    {
        var created = await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Event, "Titlu", null, null, false, null, Capacity: 5, NotifyEveryone: false), _volunteer1.Id);

        await _sut.SignUpAsync(created.Id, _volunteer1.Id);
        await _sut.SignUpAsync(created.Id, _volunteer1.Id);

        Assert.Single(_testDb.Db.OpportunitySignups);
    }

    [Fact]
    public async Task SignUpAsync_AtCapacity_ReturnsConflictForNewSignup()
    {
        var created = await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Event, "Titlu", null, null, false, null, Capacity: 1, NotifyEveryone: false), _volunteer1.Id);
        await _sut.SignUpAsync(created.Id, _volunteer1.Id);

        var result = await _sut.SignUpAsync(created.Id, _volunteer2.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CancelSignUpAsync_RemovesSignup()
    {
        var created = await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Event, "Titlu", null, null, false, null, null, NotifyEveryone: false), _volunteer1.Id);
        await _sut.SignUpAsync(created.Id, _volunteer1.Id);

        var result = await _sut.CancelSignUpAsync(created.Id, _volunteer1.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(_testDb.Db.OpportunitySignups);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotSendNotification()
    {
        var created = await _sut.CreateAsync(new CreateOpportunityRequest(
            OpportunityType.Event, "Titlu", null, null, false, null, null, NotifyEveryone: false), _volunteer1.Id);

        var result = await _sut.UpdateAsync(created.Id,
            new UpdateOpportunityRequest(OpportunityType.Event, "Titlu editat", null, null, false, null, null), _volunteer1.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Titlu editat", result.Value!.Title);
        Assert.Empty(_testDb.Db.Notifications);
    }
}
