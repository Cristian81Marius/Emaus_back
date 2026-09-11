using Emaus.Api.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Emaus.Api.Tests.Common;

public class CacheExtensionsTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void Dispose() => _cache.Dispose();

    // Notă: testat direct pe CacheExtensions, nu prin StatsService/BobStatsService — acolo,
    // repository-ul și cache-ul citesc din ACELAȘI AppDbContext, iar identity map-ul EF Core
    // ar întoarce oricum instanța deja urmărită indiferent de cache (comportament coincidental
    // al EF, nu al stratului de cache). Testând helper-ul izolat, cu un factory numărat,
    // verificăm exact contractul pe care l-am adăugat: al doilea apel NU mai cheamă factory-ul.
    [Fact]
    public async Task GetOrAddSettingsAsync_SecondCall_DoesNotInvokeFactoryAgain()
    {
        var callCount = 0;
        Task<string> Factory()
        {
            callCount++;
            return Task.FromResult($"valoare-{callCount}");
        }

        var first = await _cache.GetOrAddSettingsAsync("cheie-test", Factory);
        var second = await _cache.GetOrAddSettingsAsync("cheie-test", Factory);

        Assert.Equal(1, callCount);
        Assert.Equal("valoare-1", first);
        Assert.Equal("valoare-1", second);
    }

    [Fact]
    public async Task GetOrAddSettingsAsync_DifferentKeys_AreCachedIndependently()
    {
        var first = await _cache.GetOrAddSettingsAsync("cheie-a", () => Task.FromResult("a"));
        var second = await _cache.GetOrAddSettingsAsync("cheie-b", () => Task.FromResult("b"));

        Assert.Equal("a", first);
        Assert.Equal("b", second);
    }
}
