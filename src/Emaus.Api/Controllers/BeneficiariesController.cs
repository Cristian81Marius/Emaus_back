using Emaus.Api.Common;
using Emaus.Api.Dtos.Beneficiaries;
using Emaus.Api.Services.Beneficiaries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/beneficiaries")]
[Authorize]
public class BeneficiariesController(BeneficiaryService beneficiaryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<BeneficiaryDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 30) =>
        Ok(await beneficiaryService.GetAllAsync(search, page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BeneficiaryDto>> GetById(Guid id) =>
        (await beneficiaryService.GetByIdAsync(id)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<BeneficiaryDto>> Create(CreateBeneficiaryRequest request) =>
        (await beneficiaryService.CreateAsync(request)).ToCreatedResult(this, nameof(GetById), dto => new { id = dto.Id });

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BeneficiaryDto>> Update(Guid id, UpdateBeneficiaryRequest request) =>
        (await beneficiaryService.UpdateAsync(id, request)).ToActionResult(this);

    /// <summary>Blochează un beneficiar, cu motiv — reproduce cazul întâlnit azi în evidență
    /// ("Blocat din 15 septembrie!!"), dar cu motivul păstrat explicit, nu doar într-o notă liberă.</summary>
    [HttpPost("{id:guid}/block")]
    [Authorize(Roles = "Nucleus")]
    public async Task<IActionResult> Block(Guid id, BlockBeneficiaryRequest request) =>
        (await beneficiaryService.BlockAsync(id, request)).ToActionResult(this);

    [HttpPost("{id:guid}/unblock")]
    [Authorize(Roles = "Nucleus")]
    public async Task<IActionResult> Unblock(Guid id) =>
        (await beneficiaryService.UnblockAsync(id)).ToActionResult(this);
}
