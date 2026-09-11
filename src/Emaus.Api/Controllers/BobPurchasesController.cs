using Emaus.Api.Common;
using Emaus.Api.Dtos.Bob;
using Emaus.Api.Services.Bob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Istoric cumpărături Box of Blessing — vezi docs/API.md §13.3.</summary>
[ApiController]
[Route("api/bob/purchases")]
[Authorize]
public class BobPurchasesController(BobPurchaseService purchaseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BobDeliveryRecordDto>>> GetAll() => Ok(await purchaseService.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<BobDeliveryRecordDto>> Create(CreatePurchaseRequest request) =>
        (await purchaseService.CreateAsync(request)).ToActionResult(this);
}
