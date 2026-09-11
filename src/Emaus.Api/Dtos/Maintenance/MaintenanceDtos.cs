using Emaus.Domain;

namespace Emaus.Api.Dtos.Maintenance;

public record MaintenanceTicketDto(
    Guid Id, Guid PropertyId, string PropertyAddress, MaintenanceTicketType Type, string Description,
    MaintenanceTicketPriority Priority, MaintenanceTicketStatus Status, decimal? EstimatedCost,
    decimal? ActualCost, string? PhotoUrl, string ReportedByName, DateTime CreatedAt,
    string? AssignedToName, DateTime? ResolvedAt);

public record CreateMaintenanceTicketRequest(
    Guid PropertyId, MaintenanceTicketType Type, string Description,
    MaintenanceTicketPriority Priority, decimal? EstimatedCost, string? PhotoUrl);

public record AssignTicketRequest(Guid AssignedToUserId);

public record ResolveTicketRequest(decimal? ActualCost);
