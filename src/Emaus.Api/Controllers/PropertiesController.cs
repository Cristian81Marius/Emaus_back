using Emaus.Api.Common;
using Emaus.Api.Dtos.Properties;
using Emaus.Api.Services.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Locațiile Emaus. O proprietate (adresă) conține 1+ unități cazabile —
/// vezi schema.md pentru raționamentul din spatele acestei structuri pe două niveluri.
/// Acesta e endpoint-ul din spatele ecranului "toate locațiile, dintr-o privire".
/// Logica efectivă e în PropertyService — controllerul doar traduce request → apel → răspuns HTTP.</summary>
[ApiController]
[Route("api/properties")]
[Authorize]
public class PropertiesController(PropertyService propertyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PropertyDto>>> GetAll([FromQuery] bool includeArchived = false) =>
        Ok(await propertyService.GetAllAsync(includeArchived));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDto>> GetById(Guid id) =>
        (await propertyService.GetByIdAsync(id)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<PropertyDto>> Create(CreatePropertyRequest request) =>
        (await propertyService.CreateAsync(request)).ToCreatedResult(this, nameof(GetById), dto => new { id = dto.Id });

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<PropertyDto>> Update(Guid id, UpdatePropertyRequest request) =>
        (await propertyService.UpdateAsync(id, request)).ToActionResult(this);

    /// <summary>Nu o ștergere reală (vezi Property.IsArchived) — scoate locația din lista
    /// implicită, fără să atingă unitățile/istoricul de cazări.</summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<PropertyDto>> Archive(Guid id) =>
        (await propertyService.ArchiveAsync(id)).ToActionResult(this);

    [HttpPost("{id:guid}/unarchive")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<PropertyDto>> Unarchive(Guid id) =>
        (await propertyService.UnarchiveAsync(id)).ToActionResult(this);

    [HttpPost("{id:guid}/units")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<UnitDto>> AddUnit(Guid id, CreateUnitRequest request) =>
        (await propertyService.AddUnitAsync(id, request)).ToCreatedResult(this, nameof(GetById), new { id });
}
