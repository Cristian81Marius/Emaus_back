using System.Diagnostics;
using System.Security.Claims;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;

namespace Emaus.Api.Middleware;

/// <summary>Scrie câte o înregistrare <see cref="RequestLog"/> pentru fiecare cerere HTTP —
/// exact ce endpoint a fost apelat, de cine, cât a durat și cu ce rezultat. Citit înapoi prin
/// <c>GET /api/logs</c> (vezi LogsController) — util în dezvoltare ca să vezi direct ce cere
/// aplicația mobilă/web, fără să umbli prin consola serverului.
///
/// Înregistrat PRIMUL în pipeline (vezi Program.cs), înaintea chiar și a
/// ExceptionHandlingMiddleware, ca să prindă statusul final (inclusiv 500-urile pe care
/// acela le scrie) — cronometrul pornește aici și se oprește după ce tot restul lanțului
/// a terminat de răspuns.
///
/// Constructorul primește doar servicii singleton (RequestDelegate, ILogger); repository-ul
/// e cerut ca parametru pe InvokeAsync, nu în constructor, pentru că middleware-ul e
/// instanțiat o singură dată la pornire, dar IRepository&lt;T&gt; e scoped (o cere ASP.NET Core
/// din containerul cererii curente la fiecare apel) — un tipar standard pentru middleware
/// care are nevoie de servicii scoped.</summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IRepository<RequestLog> logs, IUnitOfWork unitOfWork)
    {
        var stopwatch = Stopwatch.StartNew();
        string? errorMessage = null;

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Excepțiile de business (404/409/400) nu ajung niciodată aici — sunt tratate
            // explicit în servicii cu ServiceResult. Doar ce scapă necontrolat trece pe-aici,
            // și numai dacă ExceptionHandlingMiddleware (înregistrat după acesta) nu-l prinde deja.
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Cazul obișnuit: ExceptionHandlingMiddleware (înregistrat după acesta, deci "mai
            // înăuntru") a prins deja excepția și a răspuns cu 500 — ea nu mai ajunge până la
            // catch-ul de mai sus. Preluăm mesajul din HttpContext.Items, unde acela îl lasă
            // special pentru noi (vezi ExceptionHandlingMiddleware).
            var finalErrorMessage = context.Items["RequestLogException"] as string ?? errorMessage;
            await TryWriteLogAsync(context, logs, unitOfWork, stopwatch.ElapsedMilliseconds, finalErrorMessage);
        }
    }

    private async Task TryWriteLogAsync(
        HttpContext context, IRepository<RequestLog> logs, IUnitOfWork unitOfWork, long durationMs, string? errorMessage)
    {
        try
        {
            var userIdRaw = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            logs.Add(new RequestLog
            {
                Id = Guid.NewGuid(),
                Method = context.Request.Method,
                Path = context.Request.Path + context.Request.QueryString,
                StatusCode = context.Response.StatusCode,
                DurationMs = durationMs,
                UserId = userIdRaw is not null ? Guid.Parse(userIdRaw) : null,
                UserName = context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                ErrorMessage = errorMessage
            });
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Jurnalizarea nu trebuie niciodată să dărâme cererea reală — dacă scrierea
            // log-ului eșuează (ex. baza de date blocată), doar notăm în consolă și mergem mai departe.
            logger.LogWarning(ex, "Nu am putut scrie jurnalul de cereri pentru {Path}", context.Request.Path);
        }
    }
}
