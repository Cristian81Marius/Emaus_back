using Emaus.Domain;

namespace Emaus.Api.Dtos.Auth;

/// <summary>Login cu telefon SAU email, în același câmp — vezi docs/API.md §2.</summary>
public record LoginRequest(string Identifier, string Password);

public record LoginResponse(string Token, UserDto User);

public record UserDto(Guid Id, string FullName, string? Phone, string? Email, UserRole Role);

/// <summary>Creare de cont nouă direct de către Nucleus (fără cerere/aprobare) — cont pornește
/// direct Active. Coexistă cu auto-înregistrarea (`POST /api/auth/register`), nu o înlocuiește:
/// util pentru cineva pe care Nucleus vrea să-l adauge direct, fără să mai aștepte o cerere.</summary>
public record CreateUserRequest(string FullName, string? Phone, string? Email, string Password, UserRole Role);

/// <summary>Editarea propriului profil — doar numele afișat. Numele deja folosit pe conținut
/// creat anterior (comentarii, "raportat de", rotația de curățenie) NU se retroactivează.</summary>
public record UpdateProfileRequest(string FullName);

/// <summary>Auto-înregistrare — cerere de cont, nu cont activ imediat. Cel puțin unul din
/// Phone/Email trebuie completat (validat în serviciu). Contul pornește `PendingApproval` —
/// login-ul rămâne blocat până decide cineva din Nucleus (`GET /api/users/pending` +
/// approve/reject).</summary>
public record RegisterRequest(string FullName, string? Phone, string? Email, string Password, UserRole RequestedRole);

/// <summary>O cerere de cont în așteptare — vezi `GET /api/users/pending`.</summary>
public record PendingUserDto(Guid Id, string FullName, string? Phone, string? Email, UserRole RequestedRole, DateTime CreatedAt);

/// <summary>`Role` opțional — dacă Nucleus vrea să corecteze rolul cerut înainte de aprobare
/// (ex. cineva a bifat greșit "Nucleus" în loc de "Voluntar"), altfel rămâne cel cerut la înregistrare.</summary>
public record ApproveUserRequest(UserRole? Role);

public record RejectUserRequest(string? Reason);
