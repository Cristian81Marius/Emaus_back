using Emaus.Api.Common;
using Emaus.Api.Dtos.Properties;
using Emaus.Api.Services.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
public class UnitsController(PropertyService propertyService) : ControllerBase
{
    /// <summary>Schimbă manual statusul unei unități — folosit mai ales pentru a o scoate
    /// din circuit (Unavailable, ex. reparație) sau a o repune disponibilă. Tranzițiile
    /// "normale" (Occupied → NeedsCleaning → Available) sunt gestionate automat de
    /// BookingsController/CleaningController; acest endpoint acoperă excepțiile.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<UnitDto>> UpdateStatus(Guid id, UpdateUnitStatusRequest request) =>
        (await propertyService.UpdateUnitStatusAsync(id, request)).ToActionResult(this);

    [HttpGet("available")]
    public async Task<ActionResult<List<UnitDto>>> GetAvailable() => Ok(await propertyService.GetAvailableUnitsAsync());
}
