using Emaus.Api.Dtos.Notifications;
using Emaus.Api.Services.Notifications;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Entities;

namespace Emaus.Api.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly RecordingPushNotificationSender _pushSender = new();
    private readonly NotificationService _sut;
    private readonly ApplicationUser _user;

    public NotificationServiceTests()
    {
        _user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "David", Phone = "0711111111", PasswordHash = "x" };
        _testDb.Db.Users.Add(_user);
        _testDb.Db.SaveChanges();

        _sut = new NotificationService(
            _testDb.Repo<ApplicationUser>(), _testDb.Repo<Notification>(), _testDb.Repo<UserPushToken>(),
            _testDb.UnitOfWork(), _pushSender);
    }

    public void Dispose() => _testDb.Dispose();

    [Fact]
    public async Task RegisterPushTokenAsync_NewToken_IsStored()
    {
        var result = await _sut.RegisterPushTokenAsync(_user.Id, new RegisterPushTokenRequest("token-abc", "android"));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_testDb.Db.UserPushTokens);
        Assert.Equal(_user.Id, stored.UserId);
        Assert.Equal("android", stored.Platform);
    }

    [Fact]
    public async Task RegisterPushTokenAsync_ExistingTokenForDifferentUser_Reassigns()
    {
        var otherUser = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Ligia", Phone = "0722222222", PasswordHash = "x" };
        _testDb.Db.Users.Add(otherUser);
        await _testDb.Db.SaveChangesAsync();
        await _sut.RegisterPushTokenAsync(otherUser.Id, new RegisterPushTokenRequest("token-shared-device", "ios"));

        await _sut.RegisterPushTokenAsync(_user.Id, new RegisterPushTokenRequest("token-shared-device", "ios"));

        var stored = Assert.Single(_testDb.Db.UserPushTokens);
        Assert.Equal(_user.Id, stored.UserId);
    }

    [Fact]
    public async Task RemovePushTokenAsync_DeletesToken()
    {
        await _sut.RegisterPushTokenAsync(_user.Id, new RegisterPushTokenRequest("token-to-remove", null));

        var result = await _sut.RemovePushTokenAsync(new RemovePushTokenRequest("token-to-remove"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_testDb.Db.UserPushTokens);
    }

    [Fact]
    public async Task RemovePushTokenAsync_UnknownToken_StillSucceeds()
    {
        var result = await _sut.RemovePushTokenAsync(new RemovePushTokenRequest("nu-exista"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NotifyUserAsync_WithRegisteredToken_SendsPushWithMessageAndData()
    {
        await _sut.RegisterPushTokenAsync(_user.Id, new RegisterPushTokenRequest("token-1", "android"));

        await _sut.NotifyUserAsync(_user.Id, NotificationType.BookingDecided, "Solicitarea a fost aprobată.",
            relatedEntityType: "Booking", relatedEntityId: Guid.NewGuid());

        var call = Assert.Single(_pushSender.Calls);
        Assert.Contains("token-1", call.Tokens);
        Assert.Equal("Solicitarea a fost aprobată.", call.Body);
        Assert.Equal("Booking", call.Data!["relatedEntityType"]);
        Assert.Equal(nameof(NotificationType.BookingDecided), call.Data["type"]);
    }

    [Fact]
    public async Task NotifyUserAsync_NoRegisteredToken_DoesNotCallPushSender()
    {
        await _sut.NotifyUserAsync(_user.Id, NotificationType.BookingDecided, "mesaj");

        Assert.Empty(_pushSender.Calls);
    }

    [Fact]
    public async Task NotifyUserAsync_AlwaysWritesInAppNotification_RegardlessOfPush()
    {
        await _sut.NotifyUserAsync(_user.Id, NotificationType.BookingDecided, "mesaj");

        var mine = await _sut.GetMineAsync(_user.Id, onlyUnread: false);
        Assert.Single(mine);
    }

    [Fact]
    public async Task NotifyUserAsync_StaleTokenReportedByFcm_IsRemovedFromDatabase()
    {
        await _sut.RegisterPushTokenAsync(_user.Id, new RegisterPushTokenRequest("token-dezinstalat", null));
        _pushSender.TokensToReportAsStale.Add("token-dezinstalat");

        await _sut.NotifyUserAsync(_user.Id, NotificationType.BookingDecided, "mesaj");

        Assert.Empty(_testDb.Db.UserPushTokens);
    }
}
