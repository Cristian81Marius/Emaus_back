using Emaus.Api.Common;
using Emaus.Api.Dtos.Auth;
using Emaus.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Autentificare cu telefon SAU email + parolă.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) =>
        (await authService.LoginAsync(request)).ToActionResult(this);

    /// <summary>Cerere de cont nouă — NU creează un cont utilizabil imediat, doar o cerere
    /// `PendingApproval`, notificată către Nucleus. Vezi UsersController pentru aprobare/
    /// respingere, sau `POST /api/users` pentru crearea directă a unui cont de către Nucleus.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request) =>
        (await authService.RegisterAsync(request)).ToActionResult(this);

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me() =>
        (await authService.GetCurrentUserAsync(User.GetUserId())).ToActionResult(this);

    [HttpPatch("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request) =>
        (await authService.UpdateProfileAsync(User.GetUserId(), request)).ToActionResult(this);
}
