namespace Emaus.Domain.Entities;

/// <summary>O înregistrare pentru fiecare cerere HTTP primită de API — "ce apelează clientul,
/// când și cu ce rezultat". Util în special în dezvoltare, ca să vezi direct din baza de date
/// (sau din <c>GET /api/logs</c>) ce endpoint-uri lovește aplicația mobilă/web, cât durează
/// și dacă a răspuns cu eroare, fără să umbli prin consola serverului.
///
/// Scrisă de <see cref="Emaus.Api.Middleware.RequestLoggingMiddleware"/>, cel mai din afară
/// strat al pipeline-ului (vezi Program.cs), ca să prindă orice cerere — inclusiv cele care
/// ies în eroare mai jos în lanț.</summary>
public class RequestLog
{
    public Guid Id { get; set; }

    /// <summary>GET, POST, PATCH, DELETE...</summary>
    public required string Method { get; set; }

    /// <summary>Calea cerută, cu query string inclus (ex. "/api/bookings?status=Active").</summary>
    public required string Path { get; set; }

    public int StatusCode { get; set; }
    public long DurationMs { get; set; }

    /// <summary>Null pentru cereri neautentificate (ex. /api/auth/login).</summary>
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>Mesajul excepției, dacă cererea a picat cu o eroare necontrolată
    /// (vezi ExceptionHandlingMiddleware) — util ca să nu mai cauți prin log-urile serverului.</summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
