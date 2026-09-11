using Emaus.Domain;

namespace Emaus.Domain.Bob;

/// <summary>Beneficiar Box of Blessing — persoană SEPARATĂ de <see cref="Emaus.Domain.Entities.Beneficiary"/>
/// (Emaus): listă de oameni diferită, câmpuri diferite (fără sold/blocare, cu mobilitate și responsabil).
/// Vezi docs/API.md §13.1.</summary>
public class BobBeneficiary
{
    public Guid Id { get; set; }

    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public BobMobility? Mobility { get; set; }
    /// <summary>Folosită și pentru link direct spre Maps în mobil (client-side) — text liber, nu
    /// legată de Locality (lista Emaus de localități normalizate nu e relevantă aici).</summary>
    public string? Address { get; set; }
    /// <summary>Text liber, NU o legătură FK spre User — vezi docs/ARCHITECTURE.md, decizia BOB
    /// confirmată: permite responsabili fără cont Emaus (ex. "George").</summary>
    public string? AssignedVolunteerName { get; set; }
    public BobBeneficiaryStatus Status { get; set; } = BobBeneficiaryStatus.Active;

    /// <summary>Bifele rundei curente de livrare — resetate manual din UI, nu există încă
    /// o rutină automată de resetare la începutul unei runde noi.</summary>
    public bool Contacted { get; set; }
    public bool Delivered { get; set; }

    public string? Notes { get; set; }
}
