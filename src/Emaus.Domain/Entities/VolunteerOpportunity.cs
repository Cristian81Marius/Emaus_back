namespace Emaus.Domain.Entities;

/// <summary>O oportunitate de implicare pentru voluntari — curățenie, eveniment, vizită
/// sau acțiune de promovare — cu înscriere directă din aplicație.</summary>
public class VolunteerOpportunity
{
    public Guid Id { get; set; }

    public OpportunityType Type { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    /// <summary>Null = "dată de stabilit" — activitățile fără dată coboară la coada listei.</summary>
    public DateTime? ScheduledAt { get; set; }
    /// <summary>Separat de existența datei — o activitate poate avea zi stabilită dar oră încă nu.</summary>
    public bool HasTime { get; set; }

    public Guid? PropertyId { get; set; }
    public Property? Property { get; set; }

    public int? Capacity { get; set; }

    public ICollection<OpportunitySignup> Signups { get; set; } = new List<OpportunitySignup>();
}
