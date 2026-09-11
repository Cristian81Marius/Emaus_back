namespace Emaus.Api.Services.Notifications;

/// <summary>Folosit când nu există cont Firebase configurat încă (`Firebase:ServiceAccountKeyPath`
/// lipsă/invalid în appsettings — vezi Program.cs) — aplicația pornește și funcționează normal,
/// notificările în-app (NotificationService) tot se salvează, doar push-ul real nu pleacă.
/// Loghează o singură dată la pornire (vezi Program.cs), nu la fiecare notificare, ca să nu
/// umple jurnalul degeaba într-un mediu unde push-ul e pur și simplu neconfigurat încă.</summary>
public class NullPushNotificationSender : IPushNotificationSender
{
    public Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, string title, string body, IReadOnlyDictionary<string, string>? data = null) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
