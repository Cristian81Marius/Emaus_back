using Emaus.Api.Common;
using Emaus.Api.Dtos.Bob;
using Emaus.Domain;
using Emaus.Domain.Bob;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Emaus.Api.Services.Bob;

/// <summary>Dashboard BOB (`GET /api/bob/stats/overview`) — vezi docs/API.md §13.4. BobSettings
/// e cache-uit (vezi CacheExtensions), la fel ca OrganizationSettings pe partea Emaus.</summary>
public class BobStatsService(
    IRepository<BobBeneficiary> beneficiaries,
    IRepository<BoxItem> items,
    IRepository<BobDeliveryRecord> records,
    IRepository<BobSettings> settingsRepo,
    IMemoryCache cache)
{
    private const string SettingsCacheKey = "bob-settings";

    public async Task<BobStatsDto> GetOverviewAsync()
    {
        var activeBeneficiaries = await beneficiaries.Query()
            .Where(b => b.Status == BobBeneficiaryStatus.Active).ToListAsync();

        var settings = await cache.GetOrAddSettingsAsync(SettingsCacheKey, () => settingsRepo.Query().FirstAsync());
        // SQLite (EF Core) nu poate traduce Sum(decimal) direct în SQL — agregăm în memorie.
        var checkedPrices = await items.Query().Where(i => i.Checked).Select(i => i.Price).ToListAsync();
        var checkedTotal = checkedPrices.Sum();

        var lastDelivery = await records.Query().OrderByDescending(r => r.Date).Select(r => (DateOnly?)r.Date).FirstOrDefaultAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endOfMonth = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        return new BobStatsDto(
            BeneficiariesCount: activeBeneficiaries.Count,
            MaxBudgetPerBox: settings.MaxBudgetPerBox,
            DaysUntilNextDelivery: endOfMonth.DayNumber - today.DayNumber,
            LastDeliveryDate: lastDelivery?.ToString("yyyy-MM-dd"),
            CurrentBoxTotal: checkedTotal,
            VolunteersInvolved: activeBeneficiaries
                .Where(b => b.AssignedVolunteerName is not null)
                .Select(b => b.AssignedVolunteerName)
                .Distinct()
                .Count());
    }
}
