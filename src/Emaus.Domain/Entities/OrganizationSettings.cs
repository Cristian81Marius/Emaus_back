namespace Emaus.Domain.Entities;

/// <summary>Rând unic (singleton) cu valorile de configurare afișate pe dashboard-ul Emaus
/// (`GET /api/stats/overview`) care NU se pot calcula din alte date — startDate e reală,
/// restul rămân placeholder până vin cifrele reale de la asociație. Un singur rând există
/// mereu în bază (creat la seed); serviciul îl citește/scrie, nu-l creează la cerere.</summary>
public class OrganizationSettings
{
    public Guid Id { get; set; }

    public DateOnly StartDate { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal EstimatedMonthlyExpenses { get; set; }
    public string? ContactPhone { get; set; }
    public string? Announcement { get; set; }
}
