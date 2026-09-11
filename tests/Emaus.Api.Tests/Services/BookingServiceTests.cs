using Emaus.Api.Dtos.Bookings;
using Emaus.Api.Services.Bookings;
using Emaus.Api.Services.Notifications;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Tests.Services;

public class BookingServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly BookingService _sut;
    private readonly ApplicationUser _nucleus;
    private readonly ApplicationUser _volunteer;
    private readonly Beneficiary _beneficiary;
    private readonly Property _property;
    private readonly Unit _unit;

    public BookingServiceTests()
    {
        _nucleus = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Alexandra", Phone = "0700000000", PasswordHash = "x", Role = UserRole.Nucleus };
        _volunteer = new ApplicationUser { Id = Guid.NewGuid(), FullName = "David", Phone = "0711111111", PasswordHash = "x", Role = UserRole.Volunteer };
        _beneficiary = new Beneficiary { Id = Guid.NewGuid(), FullName = "Test Beneficiar", Phone = "0722222222" };
        _property = new Property { Id = Guid.NewGuid(), Address = "Str. Test 1", ShortLabel = "Test 1" };
        _unit = new Unit { Id = Guid.NewGuid(), PropertyId = _property.Id, Name = "Ap. 1", Capacity = 2 };

        _testDb.Db.Users.AddRange(_nucleus, _volunteer);
        _testDb.Db.Beneficiaries.Add(_beneficiary);
        _testDb.Db.Properties.Add(_property);
        _testDb.Db.Units.Add(_unit);
        _testDb.Db.SaveChanges();

        var notificationService = new NotificationService(
            _testDb.Repo<ApplicationUser>(), _testDb.Repo<Notification>(), _testDb.Repo<UserPushToken>(),
            _testDb.UnitOfWork(), new NullPushNotificationSender());

        _sut = new BookingService(
            _testDb.Repo<Booking>(), _testDb.Repo<Beneficiary>(), _testDb.Repo<Unit>(),
            _testDb.Repo<CleaningTask>(), _testDb.Repo<CleaningAssignment>(), _testDb.Repo<ApplicationUser>(),
            _testDb.UnitOfWork(), notificationService);
    }

    public void Dispose() => _testDb.Dispose();

    private List<Notification> Notifications => _testDb.Db.Notifications.ToList();

    private async Task<BookingDto> CreatePendingBookingAsync()
    {
        var result = await _sut.CreateAsync(
            new CreateBookingRequest(_beneficiary.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15)),
            _volunteer.Id);
        Assert.True(result.IsSuccess);
        return result.Value!;
    }

    [Fact]
    public async Task CreateAsync_UnknownBeneficiary_ReturnsNotFound()
    {
        var result = await _sut.CreateAsync(
            new CreateBookingRequest(Guid.NewGuid(), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15)),
            _volunteer.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_BlockedBeneficiary_ReturnsConflict()
    {
        _beneficiary.Status = BeneficiaryStatus.Blocked;
        _beneficiary.BlockedReason = "Motiv test";
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.CreateAsync(
            new CreateBookingRequest(_beneficiary.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15)),
            _volunteer.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_Valid_NotifiesNucleusOnly()
    {
        await CreatePendingBookingAsync();

        var recipientIds = Notifications.Select(n => n.RecipientUserId).ToList();
        Assert.Contains(_nucleus.Id, recipientIds);
        Assert.DoesNotContain(_volunteer.Id, recipientIds);
        Assert.Equal(NotificationType.NewBookingRequest, Notifications.Single().Type);
    }

    [Fact]
    public async Task DecideAsync_Approve_SetsStatusAndNotifiesCreator()
    {
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, "ok"), _nucleus.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Approved, result.Value!.Status);
        Assert.Contains(Notifications, n => n.RecipientUserId == _volunteer.Id && n.Type == NotificationType.BookingDecided);
    }

    [Fact]
    public async Task DecideAsync_AlreadyDecided_ReturnsConflict()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);

        var second = await _sut.DecideAsync(booking.Id, new DecideBookingRequest(false, null), _nucleus.Id);

        Assert.False(second.IsSuccess);
    }

    [Fact]
    public async Task AllocateAsync_BeforeApproval_ReturnsConflict()
    {
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AllocateAsync_UnitNotAvailable_ReturnsConflict()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);
        _unit.Status = UnitStatus.Occupied;
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AllocateAsync_Valid_SetsUnitOccupiedAndBookingActive()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);

        var result = await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Active, result.Value!.Status);
        Assert.Equal(_unit.Id, result.Value.UnitId);
        Assert.NotNull(result.Value.ActualCheckIn);

        var unitAfter = await _testDb.Db.Units.SingleAsync(u => u.Id == _unit.Id);
        Assert.Equal(UnitStatus.Occupied, unitAfter.Status);
    }

    [Fact]
    public async Task CheckOutAsync_OnlyFromActive_ReturnsConflictOtherwise()
    {
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.CheckOutAsync(booking.Id, new CheckOutRequest(null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CheckOutAsync_Valid_SetsUnitNeedsCleaningAndCreatesCleaningTask()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);
        await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));

        var result = await _sut.CheckOutAsync(booking.Id, new CheckOutRequest(new DateOnly(2026, 10, 20)));

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, result.Value!.Status);
        Assert.Equal(new DateOnly(2026, 10, 20), result.Value.ActualCheckOut);

        var unitAfter = await _testDb.Db.Units.SingleAsync(u => u.Id == _unit.Id);
        Assert.Equal(UnitStatus.NeedsCleaning, unitAfter.Status);

        var task = await _testDb.Db.CleaningTasks.SingleAsync();
        Assert.Equal(_unit.Id, task.UnitId);
        Assert.Equal(CleaningTaskStatus.Pending, task.Status);
    }

    [Fact]
    public async Task CheckOutAsync_NotifiesOnlyCleaningVolunteersForThatProperty()
    {
        var otherVolunteer = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Nu e in rotatie", Phone = "0733333333", PasswordHash = "x" };
        _testDb.Db.Users.Add(otherVolunteer);
        _testDb.Db.CleaningAssignments.Add(new CleaningAssignment { Id = Guid.NewGuid(), PropertyId = _property.Id, VolunteerUserId = _volunteer.Id });
        await _testDb.Db.SaveChangesAsync();

        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);
        await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));
        _testDb.Db.Notifications.RemoveRange(_testDb.Db.Notifications);
        await _testDb.Db.SaveChangesAsync();

        await _sut.CheckOutAsync(booking.Id, new CheckOutRequest(null));

        var cleaningNotifications = Notifications.Where(n => n.Type == NotificationType.UnitNeedsCleaning).ToList();
        Assert.Single(cleaningNotifications);
        Assert.Equal(_volunteer.Id, cleaningNotifications[0].RecipientUserId);
    }

    [Fact]
    public async Task CancelAsync_AfterAllocation_ReturnsConflict()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);
        await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));

        var result = await _sut.CancelAsync(booking.Id, new CancelBookingRequest("motiv"), _nucleus.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CancelAsync_WhilePending_Succeeds()
    {
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.CancelAsync(booking.Id, new CancelBookingRequest("nu mai are nevoie"), _nucleus.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, result.Value!.Status);
    }

    [Fact]
    public async Task UpdateAsync_AfterAllocation_ReturnsConflict()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);
        await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));

        var result = await _sut.UpdateAsync(booking.Id, new UpdateBookingRequest(new DateOnly(2026, 11, 1), null, null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_WhilePending_UpdatesOnlyProvidedFields()
    {
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.UpdateAsync(booking.Id, new UpdateBookingRequest(new DateOnly(2026, 11, 1), null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 1), result.Value!.RequestedCheckIn);
        Assert.Equal(booking.RequestedCheckOut, result.Value.RequestedCheckOut);
    }

    [Fact]
    public async Task UpdateAsync_CaseManager_CanBeSetRegardlessOfStatus()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.DecideAsync(booking.Id, new DecideBookingRequest(true, null), _nucleus.Id);
        await _sut.AllocateAsync(booking.Id, new AllocateUnitRequest(_unit.Id));

        var result = await _sut.UpdateAsync(booking.Id, new UpdateBookingRequest(null, null, _volunteer.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(_volunteer.Id, result.Value!.CaseManagerUserId);
        Assert.Equal("David", result.Value.CaseManagerName);
    }

    [Fact]
    public async Task UpdateAsync_CaseManager_UnknownUser_ReturnsNotFound()
    {
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.UpdateAsync(booking.Id, new UpdateBookingRequest(null, null, Guid.NewGuid()));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GenerateContractPdfAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.GenerateContractPdfAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GenerateContractPdfAsync_Valid_ReturnsNonEmptyPdf()
    {
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.GenerateContractPdfAsync(booking.Id);

        Assert.True(result.IsSuccess);
        var bytes = result.Value!;
        Assert.True(bytes.Length > 1000, "PDF-ul generat pare prea mic pentru un contract de 5 pagini.");
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }

    [Fact]
    public async Task GenerateContractPdfAsync_MissingBeneficiaryData_DoesNotThrow()
    {
        // Beneficiarul din CreatePendingBookingAsync n-are Address/IdCardSeries/etc. — trebuie
        // să genereze oricum PDF-ul (câmpurile lipsă apar ca linie de completat manual).
        var booking = await CreatePendingBookingAsync();

        var result = await _sut.GenerateContractPdfAsync(booking.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GenerateContractPdfAsync_WithCaseManager_Succeeds()
    {
        var booking = await CreatePendingBookingAsync();
        await _sut.UpdateAsync(booking.Id, new UpdateBookingRequest(null, null, _nucleus.Id));

        var result = await _sut.GenerateContractPdfAsync(booking.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetAllAsync_ComputesBeneficiaryPhoneLiveFromBeneficiary()
    {
        await CreatePendingBookingAsync();
        _beneficiary.Phone = "0700009999";
        await _testDb.Db.SaveChangesAsync();

        var all = await _sut.GetAllAsync(status: null, unitId: null, propertyId: null);

        Assert.Equal("0700009999", all.Single().BeneficiaryPhone);
    }
}
