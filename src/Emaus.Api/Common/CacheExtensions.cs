using Microsoft.Extensions.Caching.Memory;

namespace Emaus.Api.Common;

/// <summary>Cache scurt pentru rânduri de configurare singleton (ex. OrganizationSettings,
/// BobSettings) — citite la fiecare încărcare de dashboard, dar scrise azi doar din seed
/// (nu există încă un ecran de editare). TTL fix, nu invalidare pe scriere: dacă apare un
/// endpoint de editare pe oricare din ele, apelați `cache.Remove(cheia)` imediat după
/// `SaveChangesAsync()`, ca schimbarea să se vadă fără să aștepte expirarea.</summary>
public static class CacheExtensions
{
    private static readonly TimeSpan SettingsTtl = TimeSpan.FromMinutes(10);

    public static Task<T> GetOrAddSettingsAsync<T>(this IMemoryCache cache, string key, Func<Task<T>> load) where T : class =>
        cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = SettingsTtl;
            return await load();
        })!;
}
