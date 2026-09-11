using Emaus.Api.Common;
using Emaus.Api.Dtos.Notifications;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Notifications;

/// <summary>Creează notificările descrise în planul aplicației — fiecare eveniment important
/// (solicitare nouă, sesizare asignată etc.) ajunge la rolul care trebuie să acționeze.
/// Rândul din tabelul Notification (citit prin polling, `GET /api/notifications/mine`) se scrie
/// mereu; push-ul real (FCM, prin <see cref="IPushNotificationSender"/>) e cel mai bun efort —
/// dacă push-ul nu e configurat (<see cref="NullPushNotificationSender"/>) sau eșuează, restul
/// fluxului (ex. aprobarea unei cazări) nu trebuie afectat.
///
/// E singurul "serviciu" apelat direct de alte servicii (nu de controllere) — reflectă
/// exact rolul lui: o unealtă transversală (notifică pe X despre Y), nu un flux propriu.</summary>
public class NotificationService(
    IRepository<ApplicationUser> users,
    IRepository<Notification> notifications,
    IRepository<UserPushToken> pushTokens,
    IUnitOfWork unitOfWork,
    IPushNotificationSender pushSender)
{
    private const string PushTitle = "Emaus";

    public async Task NotifyRoleAsync(UserRole role, NotificationType type, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        var recipients = await users.Query()
            .Where(u => u.Role == role && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        await NotifyUsersAsync(recipients, type, message, relatedEntityType, relatedEntityId);
    }

    public async Task NotifyUserAsync(Guid userId, NotificationType type, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
        => await NotifyUsersAsync([userId], type, message, relatedEntityType, relatedEntityId);

    public async Task NotifyUsersAsync(IEnumerable<Guid> userIds, NotificationType type, string message,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        var userIdList = userIds.ToList();
        if (userIdList.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var userId in userIdList)
        {
            notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = userId,
                Type = type,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                CreatedAt = now
            });
        }
        await unitOfWork.SaveChangesAsync();

        await SendPushAsync(userIdList, type, message, relatedEntityType, relatedEntityId);
    }

    public async Task<List<NotificationDto>> GetMineAsync(Guid userId, bool onlyUnread)
    {
        var query = notifications.Query().Where(n => n.RecipientUserId == userId);
        if (onlyUnread) query = query.Where(n => !n.IsRead);

        var list = await query.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync();
        return list.Select(n => new NotificationDto(
            n.Id, n.Type, n.Message, n.RelatedEntityType, n.RelatedEntityId, n.IsRead, n.CreatedAt)).ToList();
    }

    public async Task<ServiceResult> MarkReadAsync(Guid id, Guid userId)
    {
        var notification = await notifications.Query().SingleOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);
        if (notification is null) return ServiceResult.NotFound("Notificarea nu există.");

        notification.IsRead = true;
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    /// <summary>Idempotent — un token aparține unui singur dispozitiv/instalare, nu unui singur
    /// utilizator pe viață: dacă apare din nou legat de alt cont (device resetat, alt login pe
    /// același telefon), se reasignează, nu se duplică.</summary>
    public async Task<ServiceResult> RegisterPushTokenAsync(Guid userId, RegisterPushTokenRequest request)
    {
        var existing = await pushTokens.Query().SingleOrDefaultAsync(t => t.Token == request.Token);
        if (existing is not null)
        {
            existing.UserId = userId;
            existing.Platform = request.Platform;
        }
        else
        {
            pushTokens.Add(new UserPushToken { Id = Guid.NewGuid(), UserId = userId, Token = request.Token, Platform = request.Platform });
        }
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    /// <summary>Apelat la logout — nu contează al cui e tokenul (utilizatorul curent l-a scos
    /// de pe dispozitivul propriu), doar că nu mai trebuie folosit.</summary>
    public async Task<ServiceResult> RemovePushTokenAsync(RemovePushTokenRequest request)
    {
        var existing = await pushTokens.Query().SingleOrDefaultAsync(t => t.Token == request.Token);
        if (existing is not null)
        {
            pushTokens.Remove(existing);
            await unitOfWork.SaveChangesAsync();
        }
        return ServiceResult.Ok();
    }

    private async Task SendPushAsync(
        List<Guid> userIdList, NotificationType type, string message, string? relatedEntityType, Guid? relatedEntityId)
    {
        var tokens = await pushTokens.Query()
            .Where(t => userIdList.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync();
        if (tokens.Count == 0) return;

        var data = new Dictionary<string, string> { ["type"] = type.ToString() };
        if (relatedEntityType is not null) data["relatedEntityType"] = relatedEntityType;
        if (relatedEntityId is not null) data["relatedEntityId"] = relatedEntityId.Value.ToString();

        var staleTokens = await pushSender.SendAsync(tokens, PushTitle, message, data);
        if (staleTokens.Count == 0) return;

        var toRemove = await pushTokens.Query().Where(t => staleTokens.Contains(t.Token)).ToListAsync();
        foreach (var token in toRemove) pushTokens.Remove(token);
        await unitOfWork.SaveChangesAsync();
    }
}
