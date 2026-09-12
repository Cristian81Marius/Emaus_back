using Emaus.Api.Dtos.Properties;
using Emaus.Api.Services.Notifications;
using Emaus.Api.Services.Properties;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Entities;

namespace Emaus.Api.Tests.Services;

public class PropertyServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly PropertyService _sut;
    private readonly ApplicationUser _cleaningVolunteer;
    private readonly ApplicationUser _unrelatedVolunteer;
    private readonly Property _property;
    private readonly Unit _unit;

    public PropertyServiceTests()
    {
        _cleaningVolunteer = new ApplicationUser { Id = Guid.NewGuid(), FullName = "David", Phone = "0711111111", PasswordHash = "x" };
        _unrelatedVolunteer = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Altcineva", Phone = "0722222222", PasswordHash = "x" };
        _property = new Property { Id = Guid.NewGuid(), Address = "Str. Test 1", ShortLabel = "Test 1" };
        _unit = new Unit { Id = Guid.NewGuid(), PropertyId = _property.Id, Name = "Ap. 1", Capacity = 2 };

        _testDb.Db.Users.AddRange(_cleaningVolunteer, _unrelatedVolunteer);
        _testDb.Db.Properties.Add(_property);
        _testDb.Db.Units.Add(_unit);
        _testDb.Db.CleaningAssignments.Add(new CleaningAssignment { Id = Guid.NewGuid(), PropertyId = _property.Id, VolunteerUserId = _cleaningVolunteer.Id });
        _testDb.Db.SaveChanges();

        var notificationService = new NotificationService(
            _testDb.Repo<ApplicationUser>(), _testDb.Repo<Notification>(), _testDb.Repo<UserPushToken>(),
            _testDb.UnitOfWork(), new NullPushNotificationSender());
        _sut = new PropertyService(_testDb.Repo<Property>(), _testDb.Repo<Unit>(), _testDb.Repo<CleaningAssignment>(), _testDb.UnitOfWork(), notificationService);
    }

    public void Dispose() => _testDb.Dispose();

    [Fact]
    public async Task UpdateUnitStatusAsync_TransitionIntoNeedsCleaning_NotifiesOnlyAssignedVolunteers()
    {
        var result = await _sut.UpdateUnitStatusAsync(_unit.Id, new UpdateUnitStatusRequest(UnitStatus.NeedsCleaning, null));

        Assert.True(result.IsSuccess);
        var notification = Assert.Single(_testDb.Db.Notifications);
        Assert.Equal(_cleaningVolunteer.Id, notification.RecipientUserId);
        Assert.Equal(NotificationType.UnitNeedsCleaning, notification.Type);
    }

    [Fact]
    public async Task UpdateUnitStatusAsync_AlreadyNeedsCleaning_DoesNotNotifyAgain()
    {
        await _sut.UpdateUnitStatusAsync(_unit.Id, new UpdateUnitStatusRequest(UnitStatus.NeedsCleaning, null));
        _testDb.Db.Notifications.RemoveRange(_testDb.Db.Notifications);
        await _testDb.Db.SaveChangesAsync();

        // status set again while already NeedsCleaning — a re-save shouldn't re-notify
        var result = await _sut.UpdateUnitStatusAsync(_unit.Id, new UpdateUnitStatusRequest(UnitStatus.NeedsCleaning, "notă nouă"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_testDb.Db.Notifications);
    }

    [Fact]
    public async Task UpdateUnitStatusAsync_TransitionToOtherStatus_DoesNotNotify()
    {
        var result = await _sut.UpdateUnitStatusAsync(_unit.Id, new UpdateUnitStatusRequest(UnitStatus.Unavailable, "în reparație"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_testDb.Db.Notifications);
    }

    [Fact]
    public async Task UpdateAsync_OnlyChangesProvidedFields()
    {
        var result = await _sut.UpdateAsync(_property.Id, new UpdatePropertyRequest(
            Address: null, ShortLabel: null, Notes: null, Interfon: null,
            KeyHolders: ["Alexandra", "David"], KeyNotes: "1 cheie la beneficiari"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Str. Test 1", result.Value!.Address);
        Assert.Equal(["Alexandra", "David"], result.Value.KeyHolders);
        Assert.Equal("1 cheie la beneficiari", result.Value.KeyNotes);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdatePropertyRequest(null, null, null, null, null, null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetAllAsync_ByDefault_ExcludesArchivedProperties()
    {
        var archived = new Property { Id = Guid.NewGuid(), Address = "Str. Arhivată 1", ShortLabel = "Arhivată", IsArchived = true };
        _testDb.Db.Properties.Add(archived);
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        Assert.DoesNotContain(result, p => p.Id == archived.Id);
        Assert.Contains(result, p => p.Id == _property.Id);
    }

    [Fact]
    public async Task GetAllAsync_IncludeArchivedTrue_ReturnsArchivedToo()
    {
        var archived = new Property { Id = Guid.NewGuid(), Address = "Str. Arhivată 1", ShortLabel = "Arhivată", IsArchived = true };
        _testDb.Db.Properties.Add(archived);
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(includeArchived: true);

        Assert.Contains(result, p => p.Id == archived.Id);
    }

    [Fact]
    public async Task ArchiveAsync_SetsIsArchivedTrue_AndKeepsUnitsUntouched()
    {
        var result = await _sut.ArchiveAsync(_property.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsArchived);
        Assert.Single(result.Value.Units);
    }

    [Fact]
    public async Task UnarchiveAsync_SetsIsArchivedFalse()
    {
        await _sut.ArchiveAsync(_property.Id);

        var result = await _sut.UnarchiveAsync(_property.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsArchived);
    }

    [Fact]
    public async Task ArchiveAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.ArchiveAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }
}
