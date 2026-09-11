using FirebaseAdmin.Messaging;

namespace Emaus.Api.Services.Notifications;

/// <summary>Trimite prin Firebase Cloud Messaging — activ doar dacă `FirebaseApp.DefaultInstance`
/// a fost inițializat cu succes în Program.cs (cont Firebase configurat prin
/// `Firebase:ServiceAccountKeyPath`); altfel se înregistrează <see cref="NullPushNotificationSender"/>.</summary>
public class FcmPushNotificationSender(ILogger<FcmPushNotificationSender> logger) : IPushNotificationSender
{
    private const int MaxTokensPerRequest = 500; // limita FCM per MulticastMessage

    public async Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, string title, string body, IReadOnlyDictionary<string, string>? data = null)
    {
        if (tokens.Count == 0) return [];

        var staleTokens = new List<string>();
        var messaging = FirebaseMessaging.DefaultInstance;

        for (var offset = 0; offset < tokens.Count; offset += MaxTokensPerRequest)
        {
            var batch = tokens.Skip(offset).Take(MaxTokensPerRequest).ToList();
            // MulticastMessage.Tokens e marcat obsolete în SDK-ul mai nou (recomandă `Fids`,
            // legat de Firebase Installations, o suprafață API diferită de tokenul de
            // înregistrare FCM pe care îl stocăm noi/îl obține mobilul) — rămâne calea corectă
            // pentru tokenuri de înregistrare clasice.
#pragma warning disable CS0618
            var message = new MulticastMessage
            {
                Tokens = batch,
                Notification = new Notification { Title = title, Body = body },
                Data = data
            };
#pragma warning restore CS0618

            BatchResponse response;
            try
            {
                response = await messaging.SendEachForMulticastAsync(message);
            }
            catch (Exception ex)
            {
                // O eroare la nivel de request (ex. credențiale respinse) nu trebuie să dărâme
                // fluxul care a declanșat notificarea (ex. o cazare aprobată) — doar notăm.
                logger.LogWarning(ex, "Trimiterea push către FCM a eșuat pentru un lot de {Count} tokenuri.", batch.Count);
                continue;
            }

            for (var i = 0; i < response.Responses.Count; i++)
            {
                var result = response.Responses[i];
                if (!result.IsSuccess && result.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    staleTokens.Add(batch[i]);
                }
            }
        }

        return staleTokens;
    }
}
