using Emaus.Api.Common;
using Emaus.Api.Dtos.Stats;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Emaus.Api.Services.Stats;

/// <summary>Dashboard-ul Emaus (`GET /api/stats/overview`) — vezi docs/API.md §11. Majoritatea
/// cifrelor se calculează live din Property/Unit/Booking/User; ultimele 4 vin dintr-un singur
/// rând de configurare (<see cref="OrganizationSettings"/>, seed-uit o singură dată) pentru
/// că nu se pot deriva din alte date — cache-uit (vezi CacheExtensions) ca ecranul de
/// dashboard, deschis des, să nu mai citească acest rând din bază la fiecare încărcare.</summary>
public class StatsService(
    IRepository<Property> properties,
    IRepository<Unit> units,
    IRepository<ApplicationUser> users,
    IRepository<Booking> bookings,
    IRepository<OrganizationSettings> settingsRepo,
    IMemoryCache cache)
{
    private const string SettingsCacheKey = "org-settings";

    public async Task<OverviewStatsDto> GetOverviewAsync()
    {
        var propertyList = await properties.Query().ToListAsync();
        var unitList = await units.Query().ToListAsync();
        var volunteersCount = await users.Query().CountAsync(u => u.Role == UserRole.Volunteer && u.IsActive);

        var currentYear = DateTime.UtcNow.Year;
        var beneficiariesThisYear = await bookings.Query()
            .Where(b => (b.Status == BookingStatus.Active || b.Status == BookingStatus.Completed)
                && b.ActualCheckIn != null && b.ActualCheckIn.Value.Year == currentYear)
            .Select(b => b.BeneficiaryId)
            .Distinct()
            .CountAsync();

        var settings = await cache.GetOrAddSettingsAsync(SettingsCacheKey, () => settingsRepo.Query().FirstAsync());

        var lifetimeStayDays = propertyList.Sum(p => p.LifetimeStayDays ?? 0);
        var lifetimeBookingsCompleted = propertyList.Sum(p => p.LifetimeBookingsCompleted ?? 0);

        return new OverviewStatsDto(
            LocationsCount: propertyList.Count,
            UnitsCount: unitList.Count,
            OccupiedUnitsCount: unitList.Count(u => u.Status == UnitStatus.Occupied),
            LifetimeStayDays: lifetimeStayDays,
            LifetimeStayYears: Math.Round(lifetimeStayDays / 365.0, 1),
            LifetimeBookingsCompleted: lifetimeBookingsCompleted,
            VolunteersCount: volunteersCount,
            BeneficiariesThisYear: beneficiariesThisYear,
            StartDate: settings.StartDate.ToString("yyyy-MM-dd"),
            CurrentBalance: settings.CurrentBalance,
            EstimatedMonthlyExpenses: settings.EstimatedMonthlyExpenses,
            ContactPhone: settings.ContactPhone,
            Announcement: settings.Announcement);
    }
}
