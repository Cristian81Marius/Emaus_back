using Emaus.Domain;

namespace Emaus.Api.Dtos.Properties;

public record UnitDto(Guid Id, Guid PropertyId, string Name, int Capacity, UnitStatus Status, string? StatusNotes);

public record PropertyDto(
    Guid Id, string Address, string ShortLabel, bool IsTemporary, string? Notes,
    string? Interfon, List<string> KeyHolders, string? KeyNotes,
    int? LifetimeStayDays, int? LifetimeBookingsCompleted, List<UnitDto> Units);

public record CreatePropertyRequest(
    string Address, string ShortLabel, bool IsTemporary, string? Notes,
    string? Interfon, List<string>? KeyHolders, string? KeyNotes,
    int? LifetimeStayDays, int? LifetimeBookingsCompleted);

/// <summary>Editează identitatea locației — NU statusul unităților (asta e PATCH /api/units/{id}/status).</summary>
public record UpdatePropertyRequest(
    string? Address, string? ShortLabel, string? Notes,
    string? Interfon, List<string>? KeyHolders, string? KeyNotes);

public record CreateUnitRequest(string Name, int Capacity);

public record UpdateUnitStatusRequest(UnitStatus Status, string? StatusNotes);
