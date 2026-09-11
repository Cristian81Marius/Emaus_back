namespace Emaus.Domain.Entities;

/// <summary>Persoana găzduită. Câmpurile preiau exact ce se completează azi în foaia
/// fiecărui apartament (nume, domiciliu, telefon, vârstă, situație materială, sursă, observații),
/// plus statusul de blocare și soldul de cont urmărite azi în foaia "StatusActual".</summary>
public class Beneficiary
{
    public Guid Id { get; set; }

    public required string FullName { get; set; }
    public string? Phone { get; set; }

    public int? LocalityId { get; set; }
    public Locality? Locality { get; set; }
    /// <summary>Rezervă pentru cazuri fără localitate din listă (ex. beneficiari din altă țară,
    /// ca cei din Ucraina întâlniți în evidență) — vezi Locality.Country.</summary>
    public string? LocalityFreeText { get; set; }
    /// <summary>Adresa completă a domiciliului stabil (stradă, număr, bloc etc.) — Locality
    /// de mai sus ține doar localitatea/județul; asta e strada, cerută explicit de contractul
    /// de acordare a serviciilor sociale ("domiciliul stabil în loc. ..., jud. ..., Adresa: ...").</summary>
    public string? Address { get; set; }

    /// <summary>Seria și numărul C.I. — cerute de contractul de acordare a serviciilor sociale
    /// ("posesor al C.I. Seria...Nr...."), nu doar de fișa internă a beneficiarului.</summary>
    public string? IdCardSeries { get; set; }
    public string? IdCardNumber { get; set; }

    /// <summary>Persoana desemnată de beneficiar pentru situația în care își pierde autonomia
    /// (contract §I) — NU managerul de caz din partea asociației (acela e pe Booking, poate
    /// diferi de la o cazare la alta).</summary>
    public string? SupportPersonName { get; set; }
    public string? SupportPersonPhone { get; set; }

    public int? Age { get; set; }

    /// <summary>Text liber deliberat, nu enum — situația materială descrisă azi variază mult
    /// de la caz la caz și nu se pretează la o listă fixă.</summary>
    public string? MaterialSituation { get; set; }

    /// <summary>"Cum ați aflat despre noi?" — util pentru rapoarte despre canalele care chiar aduc oameni.</summary>
    public string? ReferralSource { get; set; }

    public BeneficiaryStatus Status { get; set; } = BeneficiaryStatus.Active;
    public string? BlockedReason { get; set; }
    public DateTime? BlockedAt { get; set; }

    /// <summary>Sold de cont per beneficiar, dacă asociația continuă să-l urmărească
    /// (văzut azi în "StatusActual" ca "Sold Cont"). Poate rămâne 0 dacă nu se folosește.</summary>
    public decimal AccountBalance { get; set; } = 0m;

    /// <summary>Date sensibile (context medical, situație materială) — de restricționat la
    /// nivel de API/UI doar pentru rolul Nucleus; vezi secțiunea de confidențialitate din plan.</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
