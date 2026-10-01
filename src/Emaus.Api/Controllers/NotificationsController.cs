using Emaus.Api.Common;
using Emaus.Api.Dtos.Notifications;
using Emaus.Api.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(NotificationService notificationService) : ControllerBase
{
    /// <summary>Notificările utilizatorului curent — clientul poate face polling periodic
    /// pe acest endpoint până se adaugă push real (vezi NotificationService).</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<NotificationDto>>> GetMine([FromQuery] bool onlyUnread = false) =>
        Ok(await notificationService.GetMineAsync(User.GetUserId(), onlyUnread));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id) =>
        (await notificationService.MarkReadAsync(id, User.GetUserId())).ToActionResult(this);

    /// <summary>Apelat de mobil după login (și după orice reînnoire de token FCM dată de sistem)
    /// — vezi NotificationService pentru cum se reasignează un token dacă apare pe alt cont.</summary>
    [HttpPost("push-token")]
    public async Task<IActionResult> RegisterPushToken(RegisterPushTokenRequest request) =>
        (await notificationService.RegisterPushTokenAsync(User.GetUserId(), request)).ToActionResult(this);

    /// <summary>Apelat la logout, ca dispozitivul să nu mai primească push pentru contul din
    /// care utilizatorul tocmai a ieșit.</summary>
    [HttpDelete("push-token")]
    public async Task<IActionResult> RemovePushToken(RemovePushTokenRequest request) =>
        (await notificationService.RemovePushTokenAsync(request)).ToActionResult(this);
}
