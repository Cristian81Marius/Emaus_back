namespace Emaus.Domain.Bob;

/// <summary>Un articol din catalogul cutiei. `Checked` = "face parte din runda de cumpărături
/// în lucru ACUM" — nu istoric (vezi <see cref="BobDeliveryRecord"/> pentru asta).
/// Vezi docs/API.md §13.2.</summary>
public class BoxItem
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }
    public BoxCategory Category { get; set; } = null!;

    public required string Name { get; set; }
    /// <summary>Prețul la raft — se editează des (lunar); acceptă zecimale cu virgulă SAU
    /// punct de la orice client, normalizate la parsare în serviciu, nu presupuse aici.</summary>
    public decimal Price { get; set; }
    public bool Checked { get; set; }
    public int SortOrder { get; set; }
}
