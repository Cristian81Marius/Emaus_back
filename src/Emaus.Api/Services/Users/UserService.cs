using Emaus.Api.Common;
using Emaus.Api.Dtos.Auth;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Users;

/// <summary>Gestionarea conturilor de voluntar/nucleu — vezi UsersController, rezervat
/// rolului Nucleus. Coexistă două căi de-a intra cineva în echipă: creare directă
/// (<see cref="CreateAsync"/>, cont pornește Active) și auto-înregistrare cu aprobare
/// (`AuthService.RegisterAsync` + <see cref="ApproveAsync"/>/<see cref="RejectAsync"/> aici).</summary>
public class UserService(IUserRepository users, IUnitOfWork unitOfWork)
{
    public async Task<List<UserDto>> GetAllAsync()
    {
        var list = await users.Query()
            .Where(u => u.IsActive && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    /// <summary>Cereri de acces în așteptare — cele mai vechi primele (ordinea firească de
    /// procesat o coadă).</summary>
    public async Task<List<PendingUserDto>> GetPendingAsync()
    {
        var list = await users.Query()
            .Where(u => u.Status == UserStatus.PendingApproval)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
        return list.Select(u => new PendingUserDto(u.Id, u.FullName, u.Phone, u.Email, u.Role, u.CreatedAt)).ToList();
    }

    /// <summary>Creare directă de cont de către Nucleus — pornește direct Active, fără cerere.</summary>
    public async Task<ServiceResult<UserDto>> CreateAsync(CreateUserRequest request)
    {
        if (request.Phone is not null && await users.Query().AnyAsync(u => u.Phone == request.Phone))
        {
            return ServiceResult<UserDto>.Conflict("Există deja un cont cu acest număr de telefon.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role,
            Status = UserStatus.Active
        };
        users.Add(user);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(ToDto(user));
    }

    /// <summary>`Role` opțional în body — dacă lipsește, rămâne rolul cerut la înregistrare.</summary>
    public async Task<ServiceResult<UserDto>> ApproveAsync(Guid id, ApproveUserRequest request)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null) return ServiceResult<UserDto>.NotFound("Cererea nu există.");
        if (user.Status != UserStatus.PendingApproval)
        {
            return ServiceResult<UserDto>.Conflict("Cererea a fost deja procesată.");
        }

        user.Status = UserStatus.Active;
        if (request.Role is not null) user.Role = request.Role.Value;
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(ToDto(user));
    }

    public async Task<ServiceResult> RejectAsync(Guid id, RejectUserRequest request)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null) return ServiceResult.NotFound("Cererea nu există.");
        if (user.Status != UserStatus.PendingApproval)
        {
            return ServiceResult.Conflict("Cererea a fost deja procesată.");
        }

        user.Status = UserStatus.Rejected;
        user.RejectionReason = request.Reason;
        user.IsActive = false;
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeactivateAsync(Guid id)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null) return ServiceResult.NotFound("Contul nu există.");

        user.IsActive = false;
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private static UserDto ToDto(ApplicationUser u) => new(u.Id, u.FullName, u.Phone, u.Email, u.Role);
}
