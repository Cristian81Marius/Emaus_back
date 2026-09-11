using Emaus.Api.Dtos.Auth;
using Emaus.Api.Services.Auth;
using Emaus.Api.Services.Notifications;
using Emaus.Api.Services.Security;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Emaus.Api.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly RecordingPushNotificationSender _pushSender = new();
    private readonly AuthService _sut;
    private readonly ApplicationUser _alexandra;

    public AuthServiceTests()
    {
        var tokenService = new JwtTokenService(Options.Create(new JwtOptions
        {
            Secret = "test-secret-at-least-32-characters-long!!",
            Issuer = "EmausTests",
            Audience = "EmausTests"
        }));
        var notificationService = new NotificationService(
            _testDb.Repo<ApplicationUser>(), _testDb.Repo<Notification>(), _testDb.Repo<UserPushToken>(),
            _testDb.UnitOfWork(), _pushSender);
        _sut = new AuthService(_testDb.Users(), _testDb.UnitOfWork(), tokenService, notificationService);

        _alexandra = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Alexandra",
            Phone = "0700000000",
            Email = "alexandra@emaus.ro",
            PasswordHash = PasswordHasher.Hash("Emaus2026!"),
            Role = UserRole.Nucleus,
            Status = UserStatus.Active
        };
        _testDb.Db.Users.Add(_alexandra);
        _testDb.Db.SaveChanges();
    }

    public void Dispose() => _testDb.Dispose();

    [Fact]
    public async Task LoginAsync_WithPhone_Succeeds()
    {
        var result = await _sut.LoginAsync(new LoginRequest("0700000000", "Emaus2026!"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Alexandra", result.Value!.User.FullName);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
    }

    [Fact]
    public async Task LoginAsync_WithEmailDifferentCase_Succeeds()
    {
        var result = await _sut.LoginAsync(new LoginRequest("ALEXANDRA@Emaus.ro", "Emaus2026!"));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Nucleus, result.Value!.User.Role);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
    {
        var result = await _sut.LoginAsync(new LoginRequest("0700000000", "parola gresita"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_UnknownIdentifier_ReturnsUnauthorized()
    {
        var result = await _sut.LoginAsync(new LoginRequest("0799999999", "orice"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsUnauthorized()
    {
        _testDb.Db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Cont dezactivat",
            Phone = "0711111111",
            PasswordHash = PasswordHasher.Hash("parola"),
            IsActive = false,
            Status = UserStatus.Active
        });
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.LoginAsync(new LoginRequest("0711111111", "parola"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_PendingApprovalUser_ReturnsUnauthorizedOnlyAfterCorrectPassword()
    {
        _testDb.Db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Cerere noua",
            Phone = "0722222222",
            PasswordHash = PasswordHasher.Hash("parola-buna"),
            Status = UserStatus.PendingApproval
        });
        await _testDb.Db.SaveChangesAsync();

        var wrongPassword = await _sut.LoginAsync(new LoginRequest("0722222222", "gresita"));
        var rightPassword = await _sut.LoginAsync(new LoginRequest("0722222222", "parola-buna"));

        Assert.False(wrongPassword.IsSuccess);
        Assert.False(rightPassword.IsSuccess);
        Assert.Equal("Contul tău așteaptă aprobare din partea Nucleului.", rightPassword.Error!.Message);
    }

    [Fact]
    public async Task LoginAsync_RejectedUser_ReturnsSpecificMessage()
    {
        _testDb.Db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Cerere respinsa",
            Phone = "0733333333",
            PasswordHash = PasswordHasher.Hash("parola"),
            Status = UserStatus.Rejected
        });
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.LoginAsync(new LoginRequest("0733333333", "parola"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Cererea ta de acces a fost respinsă.", result.Error!.Message);
    }

    [Fact]
    public async Task UpdateProfileAsync_BlankName_ReturnsValidationError()
    {
        var result = await _sut.UpdateProfileAsync(_alexandra.Id, new UpdateProfileRequest("   "));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateProfileAsync_TrimsAndUpdatesName()
    {
        var result = await _sut.UpdateProfileAsync(_alexandra.Id, new UpdateProfileRequest("  Alexandra Ionescu  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Alexandra Ionescu", result.Value!.FullName);
    }

    [Fact]
    public async Task RegisterAsync_MissingPhoneAndEmail_ReturnsValidationError()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("Nume", null, null, "parola123", UserRole.Volunteer));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_ShortPassword_ReturnsValidationError()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("Nume", "0744444444", null, "123", UserRole.Volunteer));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_DuplicatePhone_ReturnsConflict()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("Altcineva", "0700000000", null, "parola123", UserRole.Volunteer));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailCaseInsensitive_ReturnsConflict()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("Altcineva", null, "ALEXANDRA@EMAUS.RO", "parola123", UserRole.Volunteer));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_Valid_CreatesPendingAccountAndDoesNotAllowLoginYet()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("Voluntar Nou", "0755555555", null, "parola123", UserRole.Volunteer));

        Assert.True(result.IsSuccess);
        var created = _testDb.Db.Users.Single(u => u.Phone == "0755555555");
        Assert.Equal(UserStatus.PendingApproval, created.Status);

        var loginAttempt = await _sut.LoginAsync(new LoginRequest("0755555555", "parola123"));
        Assert.False(loginAttempt.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_NotifiesNucleusOnly()
    {
        var volunteer = new ApplicationUser { Id = Guid.NewGuid(), FullName = "David", Phone = "0766666666", PasswordHash = "x", Role = UserRole.Volunteer };
        _testDb.Db.Users.Add(volunteer);
        await _testDb.Db.SaveChangesAsync();

        await _sut.RegisterAsync(new RegisterRequest("Cerere Noua", "0777777777", null, "parola123", UserRole.Nucleus));

        var recipients = _testDb.Db.Notifications.Select(n => n.RecipientUserId).ToList();
        Assert.Contains(_alexandra.Id, recipients);
        Assert.DoesNotContain(volunteer.Id, recipients);
    }
}
