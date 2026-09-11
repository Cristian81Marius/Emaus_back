using System.Net;
using System.Text.Json;

namespace Emaus.Api.Middleware;

/// <summary>Prinde orice excepție necontrolată și o transformă într-un răspuns JSON consistent
/// ({ "error": "..." }), în loc să lase clientul (aplicația React) să primească o pagină HTML
/// de eroare sau o conexiune întreruptă. Excepțiile de business așteptate ar trebui totuși
/// tratate explicit în controllere cu coduri de status potrivite (400/404/409) — asta e doar
/// plasa de siguranță pentru ce scapă.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Eroare necontrolată pe {Path}", context.Request.Path);

            // Pus la dispoziția RequestLoggingMiddleware (înregistrat înaintea acestuia, deci
            // "mai în afară" — vezi Program.cs), ca să apară și în GET /api/logs, nu doar în
            // consola serverului. Fără asta, RequestLog.ErrorMessage rămâne mereu null pentru
            // orice excepție prinsă aici, pentru că ea nu mai ajunge să propage mai departe.
            context.Items["RequestLogException"] = $"{ex.GetType().Name}: {ex.Message}";

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = JsonSerializer.Serialize(new
            {
                error = "A apărut o eroare neașteptată. Încearcă din nou; dacă persistă, spune-i unui membru din nucleu."
            });
            await context.Response.WriteAsync(payload);
        }
    }
}
