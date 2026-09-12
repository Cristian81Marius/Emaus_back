using Emaus.Api.Common;
using Emaus.Api.Dtos.Documents;
using Emaus.Api.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

/// <summary>Generare de documente completate — deocamdată doar contractul de cazare (vezi
/// HousingContractPdfService). Deschis oricui e autentificat, la fel ca
/// GET /api/bookings/{id}/contract — orice voluntar poate genera/printa contractul pentru
/// semnare, nu doar Nucleus.</summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController(HousingContractPdfService housingContractPdfService) : ControllerBase
{
    [HttpPost("housing-contract/fill")]
    public IActionResult FillHousingContract(HousingContractFillRequest request)
    {
        var result = housingContractPdfService.Fill(request);
        if (!result.IsSuccess) return result.Error!.Type switch
        {
            ServiceErrorType.NotFound => NotFound(new { error = result.Error.Message }),
            _ => BadRequest(new { error = result.Error.Message }),
        };

        return File(result.Value!, "application/pdf", "contract-cazare.pdf");
    }

    /// <summary>Template-ul gol, necompletat — pentru cine vrea să-l printe și să-l
    /// completeze de mână, fără să treacă prin formularul din aplicație.</summary>
    [HttpGet("housing-contract/blank")]
    public IActionResult GetBlankHousingContract()
    {
        var result = housingContractPdfService.GetBlankTemplate();
        if (!result.IsSuccess) return NotFound(new { error = result.Error!.Message });

        return File(result.Value!, "application/pdf", "contract-cazare-necompletat.pdf");
    }
}
