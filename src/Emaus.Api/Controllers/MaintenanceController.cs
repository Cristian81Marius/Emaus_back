using Emaus.Api.Common;
using Emaus.Api.Dtos.Maintenance;
using Emaus.Api.Services.Maintenance;
using Emaus.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Sesizări de mentenanță — de la hârtie igienică la o reparație — puse la
/// dispoziția oricui are timp să le preia. Vezi secțiunea "Cheltuieli & mentenanță" din plan.</summary>
[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController(MaintenanceService maintenanceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MaintenanceTicketDto>>> GetAll([FromQuery] MaintenanceTicketStatus? status) =>
        Ok(await maintenanceService.GetAllAsync(status));

    [HttpPost]
    public async Task<ActionResult<MaintenanceTicketDto>> Create(CreateMaintenanceTicketRequest request) =>
        (await maintenanceService.CreateAsync(request, User.GetUserId())).ToCreatedResult(this, nameof(GetAll));

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<MaintenanceTicketDto>> Assign(Guid id, AssignTicketRequest request) =>
        (await maintenanceService.AssignAsync(id, request)).ToActionResult(this);

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<MaintenanceTicketDto>> Resolve(Guid id, ResolveTicketRequest request) =>
        (await maintenanceService.ResolveAsync(id, request)).ToActionResult(this);
}
