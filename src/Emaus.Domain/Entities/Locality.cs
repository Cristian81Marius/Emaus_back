namespace Emaus.Domain.Entities;

/// <summary>Listă standard de localități/județe pentru domiciliul beneficiarului.
/// Rezolvă direct o problemă văzută în evidența actuală: "Bacau" și "Bacău" apar
/// azi ca valori text diferite, ceea ce strică orice statistică geografică.
/// Poate fi populată o singură dată dintr-o listă oficială de localități din România
/// (+ o intrare generică "Altă țară" cu câmp liber pentru cazuri ca beneficiarii din Ucraina).</summary>
public class Locality
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? County { get; set; }
    public string? Country { get; set; } = "România";

    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
}
