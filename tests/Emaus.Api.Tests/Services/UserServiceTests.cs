using Emaus.Api.Dtos.Auth;
using Emaus.Api.Services.Users;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Infrastructure.Security;

namespace Emaus.Api.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_testDb.Users(), _testDb.UnitOfWork());
    }

    public void Dispose() => _testDb.Dispose();

    private ApplicationUser AddUser(string fullName, UserStatus status, bool isActive = true, DateTime? createdAt = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), FullName = fullName, Phone = Guid.NewGuid().ToString("N")[..10],
            PasswordHash = PasswordHasher.Hash("parola"), Status = status, IsActive = isActive,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        _testDb.Db.Users.Add(user);
        _testDb.Db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetAllAsync_ExcludesPendingAndRejectedAndInactive()
    {
        AddUser("Activ", UserStatus.Active);
        AddUser("In asteptare", UserStatus.PendingApproval);
        AddUser("Respins", UserStatus.Rejected);
        AddUser("Dezactivat", UserStatus.Active, isActive: false);

        var all = await _sut.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("Activ", all[0].FullName);
    }

    [Fact]
    public async Task GetPendingAsync_OrdersOldestFirst()
    {
        AddUser("Al doilea", UserStatus.PendingApproval, createdAt: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        AddUser("Primul", UserStatus.PendingApproval, createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var pending = await _sut.GetPendingAsync();

        Assert.Equal("Primul", pending[0].FullName);
        Assert.Equal("Al doilea", pending[1].FullName);
    }

    [Fact]
    public async Task ApproveAsync_SetsActive_KeepsRequestedRoleByDefault()
    {
        var user = AddUser("Cerere", UserStatus.PendingApproval);
        user.Role = UserRole.Volunteer;
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.ApproveAsync(user.Id, new ApproveUserRequest(Role: null));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Volunteer, result.Value!.Role);
    }

    [Fact]
    public async Task ApproveAsync_WithRoleOverride_ChangesRole()
    {
        var user = AddUser("Cerere", UserStatus.PendingApproval);
        user.Role = UserRole.Nucleus;
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.ApproveAsync(user.Id, new ApproveUserRequest(Role: UserRole.Volunteer));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Volunteer, result.Value!.Role);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyDecided_ReturnsConflict()
    {
        var user = AddUser("Deja activ", UserStatus.Active);

        var result = await _sut.ApproveAsync(user.Id, new ApproveUserRequest(null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RejectAsync_SetsRejectedAndInactiveAndStoresReason()
    {
        var user = AddUser("Cerere", UserStatus.PendingApproval);

        var result = await _sut.RejectAsync(user.Id, new RejectUserRequest("Date neclare"));

        Assert.True(result.IsSuccess);
        var reloaded = await _testDb.Db.Users.FindAsync(user.Id);
        Assert.Equal(UserStatus.Rejected, reloaded!.Status);
        Assert.False(reloaded.IsActive);
        Assert.Equal("Date neclare", reloaded.RejectionReason);
    }
}
