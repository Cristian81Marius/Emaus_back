using Emaus.Domain;

namespace Emaus.Api.Dtos.Cleaning;

public record CleaningTaskDto(
    Guid Id, Guid UnitId, string UnitName, string PropertyAddress, DateOnly ScheduledDate,
    CleaningTaskStatus Status, string? CompletedByName, DateTime? CompletedAt, string? Notes);

public record CompleteCleaningTaskRequest(string? Notes);

public record CleaningAssignmentDto(Guid Id, Guid PropertyId, string PropertyAddress, Guid VolunteerUserId,
    string VolunteerName, DayOfWeek? ScheduledDayOfWeek);

public record CreateCleaningAssignmentRequest(Guid PropertyId, Guid VolunteerUserId, DayOfWeek? ScheduledDayOfWeek);
