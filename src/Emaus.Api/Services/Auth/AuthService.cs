using Emaus.Api.Common;
using Emaus.Api.Dtos.Auth;
using Emaus.Api.Services.Notifications;
using Emaus.Api.Services.Security;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Auth;

public class AuthService(
    IUserRepository users, IUnitOfWork unitOfWork, JwtTokenService tokenService, NotificationService notifications)
{
    /// <summary>Parola se verifică ÎNAINTE de a spune orice despre statusul contului — altfel
    /// cineva care doar ghicește un telefon/email ar afla dacă există un cont și ce stare are,
    /// fără să știe parola.</summary>
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await users.GetByIdentifierAsync(request.Identifier);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return ServiceResult<LoginResponse>.Unauthorized("Telefon/email sau parolă greșite.");
        }

        switch (user.Status)
        {
            case UserStatus.PendingApproval:
                return ServiceResult<LoginResponse>.Unauthorized("Contul tău așteaptă aprobare din partea Nucleului.");
            case UserStatus.Rejected:
                return ServiceResult<LoginResponse>.Unauthorized("Cererea ta de acces a fost respinsă.");
        }
        if (!user.IsActive)
        {
            return ServiceResult<LoginResponse>.Unauthorized("Contul a fost dezactivat.");
        }

        var token = tokenService.CreateToken(user);
        var dto = new UserDto(user.Id, user.FullName, user.Phone, user.Email, user.Role);
        return ServiceResult<LoginResponse>.Ok(new LoginResponse(token, dto));
    }

    /// <summary>Auto-înregistrare — creează un cont `PendingApproval`, notifică Nucleus, dar NU
    /// întoarce un token: cererea trebuie aprobată înainte ca contul să poată face login.</summary>
    public async Task<ServiceResult> RegisterAsync(RegisterRequest request)
    {
        var fullName = request.FullName.Trim();
        if (fullName.Length == 0) return ServiceResult.Invalid("Numele nu poate fi gol.");

        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (phone is null && email is null)
        {
            return ServiceResult.Invalid("Completează fie telefonul, fie emailul.");
        }

        if (request.Password.Length < 6)
        {
            return ServiceResult.Invalid("Parola trebuie să aibă cel puțin 6 caractere.");
        }

        if (phone is not null && await users.Query().AnyAsync(u => u.Phone == phone))
        {
            return ServiceResult.Conflict("Există deja un cont cu acest număr de telefon.");
        }
        if (email is not null && await users.Query().AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower()))
        {
            return ServiceResult.Conflict("Există deja un cont cu acest email.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Phone = phone,
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.RequestedRole,
            Status = UserStatus.PendingApproval
        };
        users.Add(user);
        await unitOfWork.SaveChangesAsync();

        await notifications.NotifyRoleAsync(UserRole.Nucleus, NotificationType.NewUserRequest,
            $"{fullName} cere acces ca {(request.RequestedRole == UserRole.Nucleus ? "Nucleus" : "Voluntar")}.",
            nameof(ApplicationUser), user.Id);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId);
        if (user is null) return ServiceResult<UserDto>.NotFound("Contul nu mai există.");

        return ServiceResult<UserDto>.Ok(new UserDto(user.Id, user.FullName, user.Phone, user.Email, user.Role));
    }

    public async Task<ServiceResult<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var fullName = request.FullName.Trim();
        if (fullName.Length == 0) return ServiceResult<UserDto>.Invalid("Numele nu poate fi gol.");

        var user = await users.GetByIdAsync(userId);
        if (user is null) return ServiceResult<UserDto>.NotFound("Contul nu mai există.");

        user.FullName = fullName;
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(new UserDto(user.Id, user.FullName, user.Phone, user.Email, user.Role));
    }
}
