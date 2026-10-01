using Emaus.Domain;

namespace Emaus.Api.Dtos.Notifications;

public record NotificationDto(
    Guid Id, NotificationType Type, string Message, string? RelatedEntityType,
    Guid? RelatedEntityId, bool IsRead, DateTime CreatedAt);

/// <summary>Înregistrează/actualizează tokenul de push (FCM) al dispozitivului curent — idempotent,
/// apelat de mobil după login și după orice reînnoire de token dată de sistemul de operare.</summary>
public record RegisterPushTokenRequest(string Token, string? Platform);

/// <summary>Apelat la logout, ca dispozitivul să nu mai primească notificări pentru un cont
/// din care utilizatorul tocmai a ieșit.</summary>
public record RemovePushTokenRequest(string Token);

/// <summary>Rezultatul unui push de test trimis utilizatorului curent (POST /api/notifications/test-push)
/// — diagnostic: e configurat Firebase pe server? are contul tokenuri? ce a răspuns FCM pentru fiecare?</summary>
public record PushTestResultDto(bool PushConfigured, int TokenCount, List<PushTestTokenResultDto> Results);

/// <summary>`TokenEnd` = ultimele caractere ale tokenului (nu tot, nu e nevoie să-l expunem).</summary>
public record PushTestTokenResultDto(string? Platform, string TokenEnd, DateTime RegisteredAt, bool Success, string? Error);
