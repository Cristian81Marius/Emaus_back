using Emaus.Api.Services.Notifications;

namespace Emaus.Api.Tests.TestDoubles;

/// <summary>Dublură de test pentru <see cref="IPushNotificationSender"/> — reține fiecare
/// apel (fără să cheme FCM real) și poate simula tokenuri "învechite" (dezinstalate), ca să
/// testăm că NotificationService le șterge corect din bază.</summary>
public class RecordingPushNotificationSender : IPushNotificationSender
{
    public List<(IReadOnlyList<string> Tokens, string Title, string Body, IReadOnlyDictionary<string, string>? Data)> Calls { get; } = [];

    /// <summary>Tokenuri de raportat ca invalide la următorul apel — testul le populează
    /// înainte de a declanșa notificarea, ca să verifice curățarea ulterioară.</summary>
    public HashSet<string> TokensToReportAsStale { get; } = [];

    public Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, string title, string body, IReadOnlyDictionary<string, string>? data = null)
    {
        Calls.Add((tokens, title, body, data));
        var stale = tokens.Where(TokensToReportAsStale.Contains).ToList();
        return Task.FromResult<IReadOnlyList<string>>(stale);
    }
}
