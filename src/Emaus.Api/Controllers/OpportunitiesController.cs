using Emaus.Api.Common;
using Emaus.Api.Dtos.Opportunities;
using Emaus.Api.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Curățenie, evenimente, vizite, promovare — calendarul unic de oportunități
/// de implicare descris în plan, cu înscriere directă.</summary>
[ApiController]
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesController(OpportunityService opportunityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OpportunityDto>>> GetAll() =>
        Ok(await opportunityService.GetAllAsync(User.GetUserId()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OpportunityDto>> GetById(Guid id) =>
        (await opportunityService.GetByIdAsync(id, User.GetUserId())).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<OpportunityDto>> Create(CreateOpportunityRequest request) =>
        Ok(await opportunityService.CreateAsync(request, User.GetUserId()));

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<OpportunityDto>> Update(Guid id, UpdateOpportunityRequest request) =>
        (await opportunityService.UpdateAsync(id, request, User.GetUserId())).ToActionResult(this);

    [HttpPost("{id:guid}/signup")]
    public async Task<IActionResult> SignUp(Guid id) =>
        (await opportunityService.SignUpAsync(id, User.GetUserId())).ToActionResult(this);

    [HttpDelete("{id:guid}/signup")]
    public async Task<IActionResult> CancelSignUp(Guid id) =>
        (await opportunityService.CancelSignUpAsync(id, User.GetUserId())).ToActionResult(this);
}
