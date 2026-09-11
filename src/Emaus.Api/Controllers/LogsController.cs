using Emaus.Api.Dtos.Logs;
using Emaus.Api.Services.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Jurnalul cererilor HTTP către API — "ce apelează clientul (mobil/web), când și
/// cu ce rezultat" — util mai ales în dezvoltare, ca să nu mai umbli prin consola serverului.
/// Rezervat rolului Nucleus. Vezi RequestLoggingMiddleware pentru partea de scriere.</summary>
[ApiController]
[Route("api/logs")]
[Authorize(Roles = "Nucleus")]
public class LogsController(RequestLogService requestLogService) : ControllerBase
{
    /// <summary>Cele mai recente cereri, cele mai noi primele. Filtrele sunt opționale și
    /// se pot combina, ex. <c>GET /api/logs?method=POST&amp;statusCode=409&amp;take=50</c>.</summary>
    [HttpGet]
    public async Task<ActionResult<List<RequestLogDto>>> GetRecent(
        [FromQuery] string? method, [FromQuery] string? path, [FromQuery] int? statusCode, [FromQuery] int take = 100) =>
        Ok(await requestLogService.GetRecentAsync(method, path, statusCode, take));
}
