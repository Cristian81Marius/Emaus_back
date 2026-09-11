namespace Emaus.Domain.Entities;

/// <summary>Sesizare de mentenanță — de la "hârtie igienică" la "robinet stricat", pusă la
/// dispoziție oricui are timp să o preia. Legată de Property (nu de Unit), pentru că multe
/// nevoi privesc întreaga locuință, nu doar o cameră.</summary>
public class MaintenanceTicket
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public MaintenanceTicketType Type { get; set; }
    public required string Description { get; set; }
    public MaintenanceTicketPriority Priority { get; set; } = MaintenanceTicketPriority.Medium;
    public MaintenanceTicketStatus Status { get; set; } = MaintenanceTicketStatus.New;

    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? PhotoUrl { get; set; }

    public Guid ReportedByUserId { get; set; }
    public ApplicationUser ReportedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
