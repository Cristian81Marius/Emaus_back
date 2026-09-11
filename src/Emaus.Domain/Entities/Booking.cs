namespace Emaus.Domain.Entities;

/// <summary>O solicitare/cazare — nucleul fluxului descris în plan: cerere → notificare →
/// discuție → decizie → alocare → check-out automat. Perioada "solicitată" vs. "realizată"
/// e păstrată separat exact ca în evidența actuală, pentru că deseori diferă.</summary>
public class Booking
{
    public Guid Id { get; set; }

    public Guid BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;

    /// <summary>Null până la alocare (o solicitare "În așteptare" nu are încă o unitate).</summary>
    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public DateOnly RequestedCheckIn { get; set; }
    public DateOnly RequestedCheckOut { get; set; }
    public DateOnly? ActualCheckIn { get; set; }
    public DateOnly? ActualCheckOut { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.PendingApproval;

    public Guid CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? DecidedByUserId { get; set; }
    public ApplicationUser? DecidedByUser { get; set; }
    public DateTime? DecidedAt { get; set; }
    /// <summary>Motivul respingerii, sau orice notă scurtă a deciziei.</summary>
    public string? DecisionNote { get; set; }

    /// <summary>Managerul de caz desemnat pentru ACEASTĂ cazare (contractul de acordare a
    /// serviciilor sociale cere un nume+telefon per contract) — poate diferi de la o cazare la
    /// alta a aceluiași beneficiar, spre deosebire de persoana de sprijin (asta e pe Beneficiary,
    /// ține de persoană, nu de contract). Nullable — se poate desemna oricând, nu doar la creare.</summary>
    public Guid? CaseManagerUserId { get; set; }
    public ApplicationUser? CaseManagerUser { get; set; }

    public ICollection<BookingComment> Comments { get; set; } = new List<BookingComment>();
}
