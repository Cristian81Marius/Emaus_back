namespace Emaus.Domain.Entities;

/// <summary>Un utilizator intern al aplicației — voluntar sau membru al nucleului.
/// NU beneficiarii; ei nu au cont, rămân contactați telefonic/WhatsApp ca și acum.
/// Dacă folosiți ASP.NET Core Identity, această clasă poate moșteni IdentityUser&lt;Guid&gt;
/// în loc să declare Id/Email manual — lăsat simplu aici ca să fie limpede schema.</summary>
public class ApplicationUser
{
    public Guid Id { get; set; }

    public required string FullName { get; set; }
    /// <summary>Nullable — un cont poate exista doar cu email (auto-înregistrare cu un singur
    /// identificator), la fel cum login-ul acceptă telefon SAU email. Cel puțin unul din
    /// Phone/Email trebuie completat — verificat în serviciu, nu impus de schemă.</summary>
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Hash PBKDF2 (vezi Emaus.Infrastructure.Security.PasswordHasher), niciodată
    /// parola în clar. Login-ul se face cu telefon sau email + parolă.</summary>
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Volunteer;

    /// <summary>Ciclul de aprobare — separat de <see cref="IsActive"/> (dezactivarea unui cont
    /// deja aprobat, mai târziu). Un cont auto-înregistrat pornește PendingApproval și nu se
    /// poate loga până nu decide cineva din Nucleus.</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;
    /// <summary>Motivul respingerii, dacă Status == Rejected — la fel ca Beneficiary.BlockedReason.</summary>
    public string? RejectionReason { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigare inversă (opțional, util pentru interogări)
    public ICollection<Booking> BookingsRequested { get; set; } = new List<Booking>();
    public ICollection<Booking> BookingsDecided { get; set; } = new List<Booking>();
    public ICollection<MaintenanceTicket> MaintenanceTicketsAssigned { get; set; } = new List<MaintenanceTicket>();
    public ICollection<OpportunitySignup> OpportunitySignups { get; set; } = new List<OpportunitySignup>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
