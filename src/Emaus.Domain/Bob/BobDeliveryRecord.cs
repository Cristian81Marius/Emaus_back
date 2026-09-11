namespace Emaus.Domain.Bob;

/// <summary>Un rând din istoricul de cumpărături — SINGURA parte a domeniului BOB care trebuie
/// să persiste real (cerință explicită a utilizatorului). Articolele sunt un instantaneu
/// (nume + preț DIN MOMENTUL cumpărăturii), nu o referință live la <see cref="BoxItem"/> —
/// dacă prețul curent al articolului se schimbă ulterior, istoricul trebuie să rămână corect.
/// Vezi docs/API.md §13.3.</summary>
public class BobDeliveryRecord
{
    public Guid Id { get; set; }

    public DateOnly Date { get; set; }
    /// <summary>Calculat la salvare, NU recalculat ulterior — chiar dacă prețurile articolelor
    /// din instantaneu s-ar putea re-suma, totalul rămâne cel înregistrat atunci.</summary>
    public decimal Total { get; set; }
    public string? Note { get; set; }

    public ICollection<BobDeliveryRecordItem> Items { get; set; } = new List<BobDeliveryRecordItem>();
}

/// <summary>Un articol instantaneu dintr-un <see cref="BobDeliveryRecord"/> — nume + preț
/// copiate din <see cref="BoxItem"/> la momentul salvării, fără FK spre articolul original.</summary>
public class BobDeliveryRecordItem
{
    public Guid Id { get; set; }

    public Guid DeliveryRecordId { get; set; }
    public BobDeliveryRecord DeliveryRecord { get; set; } = null!;

    public required string Name { get; set; }
    public decimal Price { get; set; }
}
