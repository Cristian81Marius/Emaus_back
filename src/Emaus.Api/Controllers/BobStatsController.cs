using Emaus.Api.Dtos.Bob;
using Emaus.Api.Services.Bob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Dashboard Box of Blessing — vezi docs/API.md §13.4.</summary>
[ApiController]
[Route("api/bob/stats")]
[Authorize]
public class BobStatsController(BobStatsService statsService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<BobStatsDto>> Overview() => Ok(await statsService.GetOverviewAsync());
}
