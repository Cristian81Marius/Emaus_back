namespace Emaus.Domain.Bob;

/// <summary>O categorie din catalogul cutiei Box of Blessing (ex. "Băcănie", "Igienă & curățenie").
/// Vezi docs/API.md §13.2.</summary>
public class BoxCategory
{
    public Guid Id { get; set; }

    /// <summary>Cheie stabilă (ex. "bacanie") — folosită de client și la adăugarea unui articol
    /// nou într-o categorie existentă (`POST /api/bob/box/items`).</summary>
    public required string Key { get; set; }
    public required string Title { get; set; }
    public int SortOrder { get; set; }

    public ICollection<BoxItem> Items { get; set; } = new List<BoxItem>();
}
