namespace Emaus.Api.Services.Notifications;

/// <summary>Trimite notificări push reale (FCM), separat de notificarea în-app salvată în bază
/// (vezi NotificationService) — o interfață proprie, nu direct SDK-ul Firebase în
/// NotificationService, ca push-ul să poată lipsi complet (dezvoltare, fără cont Firebase încă)
/// fără să strice restul aplicației: vezi <see cref="NullPushNotificationSender"/>.</summary>
public interface IPushNotificationSender
{
    /// <summary>Trimite același mesaj către toate tokenurile date. Întoarce tokenurile pe care
    /// FCM le-a raportat ca definitiv invalide (dezinstalat/dezînregistrat) — apelantul trebuie
    /// să le șteargă, altfel se încearcă la nesfârșit trimiterea către un dispozitiv care nu
    /// mai există.</summary>
    Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, string title, string body, IReadOnlyDictionary<string, string>? data = null);
}
