using Emaus.Api.Common;
using Emaus.Api.Dtos.Bob;
using Emaus.Api.Services.Bob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Beneficiari Box of Blessing — vezi docs/API.md §13.1. Toate rutele deschise oricui
/// e autentificat, fără restricție de rol (decizie de produs confirmată — proiect colaborativ,
/// fără ierarhia Nucleus/Voluntar de la Emaus).</summary>
[ApiController]
[Route("api/bob/beneficiaries")]
[Authorize]
public class BobBeneficiariesController(BobBeneficiaryService beneficiaryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BobBeneficiaryDto>>> GetAll() => Ok(await beneficiaryService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BobBeneficiaryDto>> GetById(Guid id) =>
        (await beneficiaryService.GetByIdAsync(id)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<BobBeneficiaryDto>> Create(CreateBobBeneficiaryRequest request) =>
        Ok(await beneficiaryService.CreateAsync(request));

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BobBeneficiaryDto>> Update(Guid id, UpdateBobBeneficiaryRequest request) =>
        (await beneficiaryService.UpdateAsync(id, request)).ToActionResult(this);
}
