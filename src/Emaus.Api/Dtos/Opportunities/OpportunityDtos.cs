using Emaus.Domain;

namespace Emaus.Api.Dtos.Opportunities;

public record OpportunityDto(
    Guid Id, OpportunityType Type, string Title, string? Description,
    DateTime? ScheduledAt, // null = "dată de stabilit"
    bool HasTime, // separat de existența datei — are și ORĂ aleasă?
    Guid? PropertyId, string? PropertyAddress, int? Capacity, int SignedUpCount, bool CurrentUserSignedUp);

/// <summary>`NotifyEveryone` NU se stochează pe entitate — controlează doar efectul la creare
/// (notifică toți voluntarii sau nu).</summary>
public record CreateOpportunityRequest(
    OpportunityType Type, string Title, string? Description, DateTime? ScheduledAt, bool HasTime,
    Guid? PropertyId, int? Capacity, bool NotifyEveryone);

/// <summary>Aceleași câmpuri ca la creare, FĂRĂ NotifyEveryone — o editare ulterioară nu
/// retrimite nicio notificare.</summary>
public record UpdateOpportunityRequest(
    OpportunityType Type, string Title, string? Description, DateTime? ScheduledAt, bool HasTime,
    Guid? PropertyId, int? Capacity);
