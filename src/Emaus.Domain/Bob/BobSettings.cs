namespace Emaus.Domain.Bob;

/// <summary>Rând unic (singleton) cu valorile de configurare BOB care nu se calculează din alte
/// date — la fel ca <see cref="Emaus.Domain.Entities.OrganizationSettings"/> pentru Emaus.
/// Un singur rând există mereu în bază (creat la seed).</summary>
public class BobSettings
{
    public Guid Id { get; set; }

    /// <summary>Buget maxim per cutie — valoare fixă de configurare (99.28 lei din excel-ul
    /// original), NU calculată.</summary>
    public decimal MaxBudgetPerBox { get; set; }
}
