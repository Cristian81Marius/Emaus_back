using Emaus.Api.Dtos.Menu;
using Emaus.Api.Services.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/menu")]
[Authorize]
public class MenuController(MenuService menuService) : ControllerBase
{
    /// <summary>Conținutul e hardcodat în MenuService (vezi acolo) — nu se schimbă între
    /// cereri decât la un deploy nou de cod, deci fiecare client (mobil/web) poate ține
    /// răspunsul cache-uit local o oră, fără să mai bată la server la fiecare navigare
    /// între ecrane. `private`, nu `public`: răspunsul cere autentificare, deci nu trebuie
    /// stocat de un cache PARTAJAT (proxy) între useri diferiți — doar de clientul care l-a cerut.</summary>
    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public ActionResult<MenuConfigDto> Get([FromQuery] string? project) => Ok(menuService.GetMenu(project));
}
