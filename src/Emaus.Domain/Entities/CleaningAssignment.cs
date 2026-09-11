namespace Emaus.Domain.Entities;

/// <summary>Abonamentul rotativ de curățenie al unei proprietăți — echivalentul
/// "Responsabil 1..4" din foaia "Curățenie&amp;Vizite". Nu e o tură concretă
/// (asta e <see cref="CleaningTask"/>), ci lista voluntarilor care se rotesc pe adresa respectivă.</summary>
public class CleaningAssignment
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid VolunteerUserId { get; set; }
    public ApplicationUser VolunteerUser { get; set; } = null!;

    /// <summary>Ziua din săptămână alocată acestui voluntar pentru rotație, dacă se păstrează
    /// un tipar fix (ca azi); poate rămâne null dacă rotația se stabilește ad-hoc.</summary>
    public DayOfWeek? ScheduledDayOfWeek { get; set; }
}
