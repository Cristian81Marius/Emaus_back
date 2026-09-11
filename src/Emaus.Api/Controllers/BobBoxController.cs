using Emaus.Api.Common;
using Emaus.Api.Dtos.Bob;
using Emaus.Api.Services.Bob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Catalog cutie Box of Blessing — vezi docs/API.md §13.2.</summary>
[ApiController]
[Route("api/bob/box")]
[Authorize]
public class BobBoxController(BobBoxService boxService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BoxCategoryDto>>> GetCategories() => Ok(await boxService.GetCategoriesAsync());

    [HttpPatch("items/{id:guid}")]
    public async Task<ActionResult<BoxItemDto>> UpdateItem(Guid id, UpdateBoxItemRequest request) =>
        (await boxService.UpdateItemAsync(id, request)).ToActionResult(this);

    [HttpPost("items")]
    public async Task<ActionResult<BoxItemDto>> CreateItem(CreateBoxItemRequest request) =>
        (await boxService.CreateItemAsync(request)).ToActionResult(this);
}
