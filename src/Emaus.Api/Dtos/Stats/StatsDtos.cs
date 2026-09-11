namespace Emaus.Api.Dtos.Stats;

/// <summary>Vezi docs/API.md §11. Primele câmpuri se calculează live din date existente;
/// ultimele 4 sunt valori de configurare (OrganizationSettings) — placeholder până vin
/// cifrele reale de la asociație.</summary>
public record OverviewStatsDto(
    int LocationsCount, int UnitsCount, int OccupiedUnitsCount,
    int LifetimeStayDays, double LifetimeStayYears, int LifetimeBookingsCompleted,
    int VolunteersCount, int BeneficiariesThisYear,
    string StartDate, // dată simplă
    decimal CurrentBalance, decimal EstimatedMonthlyExpenses, string? ContactPhone, string? Announcement);
