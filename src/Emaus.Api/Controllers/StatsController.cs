using Emaus.Api.Dtos.Stats;
using Emaus.Api.Services.Stats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(StatsService statsService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<OverviewStatsDto>> Overview() => Ok(await statsService.GetOverviewAsync());
}
