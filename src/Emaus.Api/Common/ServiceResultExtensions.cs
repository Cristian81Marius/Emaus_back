using Microsoft.AspNetCore.Mvc;

namespace Emaus.Api.Common;

public static class ServiceResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ServiceResult<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : ToErrorResult(result.Error!, controller);

    /// <summary>Pentru POST-uri care creează o resursă — 201 cu Location, în loc de 200. Folosiți
    /// asta doar când acțiunea țintă NU are parametri de rută (ex. un `GetAll` fără `{id}`) —
    /// altfel `CreatedAtAction` aruncă "No route matches the supplied values." la runtime,
    /// pentru că nu are de unde lua valoarea lui `{id}`. Pentru orice acțiune țintă cu `{id}`,
    /// folosiți suprîncărcarea de mai jos, cu `routeValues` calculat din rezultat.</summary>
    public static ActionResult<T> ToCreatedResult<T>(this ServiceResult<T> result, ControllerBase controller,
        string actionName, object? routeValues = null) =>
        result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues, result.Value)
            : ToErrorResult(result.Error!, controller);

    /// <summary>Variantă pentru acțiuni țintă cu parametri de rută (ex. `GetById(Guid id)`) —
    /// `routeValues` se calculează din valoarea creată (de regulă `dto => new { id = dto.Id }`),
    /// ca să nu se mai poată uita, așa cum s-a întâmplat inițial la Properties/Beneficiaries/Bookings:
    /// omiterea lui `routeValues` cu o acțiune țintă ce cere `{id}` produce exact acea eroare.</summary>
    public static ActionResult<T> ToCreatedResult<T>(this ServiceResult<T> result, ControllerBase controller,
        string actionName, Func<T, object> routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues(result.Value!), result.Value)
            : ToErrorResult(result.Error!, controller);

    /// <summary>Pentru operații fără valoare de întors (ex. blocare, marcare citit) — 204 la succes.</summary>
    public static IActionResult ToActionResult(this ServiceResult result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : ToErrorResult(result.Error!, controller);

    private static ActionResult ToErrorResult(ServiceError error, ControllerBase controller) => error.Type switch
    {
        ServiceErrorType.NotFound => controller.NotFound(new { error = error.Message }),
        ServiceErrorType.Conflict => controller.Conflict(new { error = error.Message }),
        ServiceErrorType.Validation => controller.BadRequest(new { error = error.Message }),
        ServiceErrorType.Unauthorized => controller.Unauthorized(new { error = error.Message }),
        _ => controller.StatusCode(500, new { error = error.Message }),
    };
}
