namespace Emaus.Domain.Entities;

/// <summary>O apariție concretă de curățenie pentru o unitate — creată automat când unitatea
/// trece în <see cref="UnitStatus.NeedsCleaning"/> (de obicei la check-out) sau manual.
/// Când e marcată Done, unitatea trece înapoi în Available.</summary>
public class CleaningTask
{
    public Guid Id { get; set; }

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public DateOnly ScheduledDate { get; set; }
    public CleaningTaskStatus Status { get; set; } = CleaningTaskStatus.Pending;

    public Guid? CompletedByUserId { get; set; }
    public ApplicationUser? CompletedByUser { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }
}
