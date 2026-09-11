using Emaus.Api.Common;
using Emaus.Api.Dtos.Cleaning;
using Emaus.Api.Services.Cleaning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/cleaning")]
[Authorize]
public class CleaningController(CleaningService cleaningService) : ControllerBase
{
    [HttpGet("tasks")]
    public async Task<ActionResult<List<CleaningTaskDto>>> GetTasks([FromQuery] bool onlyPending = true) =>
        Ok(await cleaningService.GetTasksAsync(onlyPending));

    /// <summary>Marchează tura ca finalizată — unitatea revine automat "Liber & igienizat".</summary>
    [HttpPost("tasks/{id:guid}/complete")]
    public async Task<ActionResult<CleaningTaskDto>> Complete(Guid id, CompleteCleaningTaskRequest request) =>
        (await cleaningService.CompleteAsync(id, request, User.GetUserId())).ToActionResult(this);

    [HttpGet("assignments")]
    public async Task<ActionResult<List<CleaningAssignmentDto>>> GetAssignments([FromQuery] Guid? propertyId) =>
        Ok(await cleaningService.GetAssignmentsAsync(propertyId));

    /// <summary>Adaugă un voluntar în rotația de curățenie a unei adrese — echivalentul
    /// "Responsabil 1..4" din evidența actuală.</summary>
    [HttpPost("assignments")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<CleaningAssignmentDto>> CreateAssignment(CreateCleaningAssignmentRequest request) =>
        (await cleaningService.CreateAssignmentAsync(request)).ToActionResult(this);
}
