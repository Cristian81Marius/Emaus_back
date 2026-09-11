using Emaus.Api.Common;
using Emaus.Api.Dtos.Bookings;
using Emaus.Api.Services.Bookings;
using Emaus.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Fluxul central al aplicației — vezi secțiunea "Flux: solicitare nouă" din plan:
/// cerere → notificare nucleu → comentarii → decizie → alocare unitate → check-out automat
/// spre curățenie. Logica fiecărui pas trăiește în BookingService; controllerul doar
/// traduce request → apel → răspuns HTTP.</summary>
[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(BookingService bookingService) : ControllerBase
{
    /// <summary>`unitId`/`propertyId` — folosite de ecranul unei locații ca să arate cine
    /// stă acum acolo + istoricul (vezi comentariul de pe BookingService.GetAllAsync).
    /// `beneficiaryId` — folosit de fișa unui beneficiar ca să-i arate tot istoricul.</summary>
    [HttpGet]
    public async Task<ActionResult<List<BookingDto>>> GetAll(
        [FromQuery] BookingStatus? status, [FromQuery] Guid? unitId, [FromQuery] Guid? propertyId,
        [FromQuery] Guid? beneficiaryId) =>
        Ok(await bookingService.GetAllAsync(status, unitId, propertyId, beneficiaryId));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetById(Guid id) =>
        (await bookingService.GetByIdAsync(id)).ToActionResult(this);

    /// <summary>PDF-ul "Contract de acordare a serviciilor sociale", completat cu datele
    /// cazării/beneficiarului — vezi BookingContractDocument. Deschis oricui e autentificat
    /// (nu doar Nucleus): managerul de caz desemnat pe o cazare poate fi un voluntar, care
    /// trebuie să poată genera și printa contractul pentru semnare.</summary>
    [HttpGet("{id:guid}/contract")]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var result = await bookingService.GenerateContractPdfAsync(id);
        if (!result.IsSuccess) return NotFound(new { error = result.Error!.Message });

        return File(result.Value!, "application/pdf", $"contract-{id}.pdf");
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request) =>
        (await bookingService.CreateAsync(request, User.GetUserId())).ToCreatedResult(this, nameof(GetById), dto => new { id = dto.Id });

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<BookingDto>> Update(Guid id, UpdateBookingRequest request) =>
        (await bookingService.UpdateAsync(id, request)).ToActionResult(this);

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<BookingCommentDto>> AddComment(Guid id, AddCommentRequest request) =>
        (await bookingService.AddCommentAsync(id, request, User.GetUserId())).ToActionResult(this);

    [HttpPost("{id:guid}/decide")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<BookingDto>> Decide(Guid id, DecideBookingRequest request) =>
        (await bookingService.DecideAsync(id, request, User.GetUserId())).ToActionResult(this);

    /// <summary>Alocă o unitate liberă unei solicitări aprobate. Pas separat de decizie
    /// (vezi comentariul de pe AllocateUnitRequest) — unitatea trebuie să fie Available acum.</summary>
    [HttpPost("{id:guid}/allocate")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<BookingDto>> Allocate(Guid id, AllocateUnitRequest request) =>
        (await bookingService.AllocateAsync(id, request)).ToActionResult(this);

    /// <summary>La check-out, unitatea trece automat în NeedsCleaning și se deschide o tură
    /// de curățenie — vezi secțiunea "Curățenie & chei" din plan.</summary>
    [HttpPost("{id:guid}/checkout")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<BookingDto>> CheckOut(Guid id, CheckOutRequest request) =>
        (await bookingService.CheckOutAsync(id, request)).ToActionResult(this);

    /// <summary>Anulează o solicitare care încă n-a ajuns la o unitate. Rezervat rolului
    /// Nucleus, la fel ca restul deciziilor pe o solicitare.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Nucleus")]
    public async Task<ActionResult<BookingDto>> Cancel(Guid id, CancelBookingRequest request) =>
        (await bookingService.CancelAsync(id, request, User.GetUserId())).ToActionResult(this);
}
