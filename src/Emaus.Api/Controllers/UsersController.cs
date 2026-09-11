using Emaus.Api.Common;
using Emaus.Api.Dtos.Auth;
using Emaus.Api.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Nucleus")]
public class UsersController(UserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll() => Ok(await userService.GetAllAsync());

    /// <summary>Cereri de auto-înregistrare în așteptare — vezi AuthController.Register.</summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingUserDto>>> GetPending() => Ok(await userService.GetPendingAsync());

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request) =>
        (await userService.CreateAsync(request)).ToCreatedResult(this, nameof(GetAll));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<UserDto>> Approve(Guid id, ApproveUserRequest request) =>
        (await userService.ApproveAsync(id, request)).ToActionResult(this);

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectUserRequest request) =>
        (await userService.RejectAsync(id, request)).ToActionResult(this);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id) =>
        (await userService.DeactivateAsync(id)).ToActionResult(this);
}
