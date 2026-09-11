using Emaus.Api.Services.Stats;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Emaus.Api.Tests.Services;

public class StatsServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly StatsService _sut;

    public StatsServiceTests()
    {
        _testDb.Db.OrganizationSettings.Add(new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            StartDate = new DateOnly(2022, 1, 24),
            CurrentBalance = 100m,
            EstimatedMonthlyExpenses = 50m
        });
        _testDb.Db.SaveChanges();

        _sut = new StatsService(
            _testDb.Repo<Property>(), _testDb.Repo<Unit>(), _testDb.Repo<ApplicationUser>(),
            _testDb.Repo<Booking>(), _testDb.Repo<OrganizationSettings>(), _cache);
    }

    public void Dispose()
    {
        _testDb.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task GetOverviewAsync_ReturnsConfiguredSettingsValues()
    {
        var stats = await _sut.GetOverviewAsync();

        Assert.Equal("2022-01-24", stats.StartDate);
        Assert.Equal(100m, stats.CurrentBalance);
    }

}
