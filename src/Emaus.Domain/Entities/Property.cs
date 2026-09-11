namespace Emaus.Domain.Entities;

/// <summary>O adresă fizică deținută/folosită de Emaus (ex. "str. Traian Popovici 132, B3D, sc A, et. 5, ap28").
/// O proprietate poate avea una sau mai multe <see cref="Unit"/> cazabile — de exemplu apartamentul
/// de pe Traian Popovici e împărțit azi în "dormitor" și "sufragerie", fiecare cu status propriu,
/// dar cheile și responsabilii de curățenie sunt urmăriți la nivel de adresă, nu per cameră
/// (exact cum arată foaia "Curățenie&amp;Vizite" din evidența actuală).</summary>
public class Property
{
    public Guid Id { get; set; }

    public required string Address { get; set; }
    /// <summary>Etichetă scurtă pentru liste (adresa completă apare doar pe fișa locației).</summary>
    public required string ShortLabel { get; set; }
    /// <summary>Ex: "Airbnb/Temporar" pentru locațiile din foaia "Loc.Intermediar" — utile de marcat
    /// separat pentru că nu sunt locații fixe ale asociației.</summary>
    public bool IsTemporary { get; set; } = false;
    public string? Notes { get; set; }

    public string? Interfon { get; set; }
    /// <summary>Cine are cheile locației — listă simplă de nume, NU o legătură reală spre
    /// utilizatori (nu toți cei care țin o cheie au neapărat cont în aplicație). Stocată ca JSON
    /// (vezi conversia din AppDbContext) — nu merită un tabel separat pentru o listă de câteva nume.</summary>
    public List<string> KeyHolders { get; set; } = new();
    public string? KeyNotes { get; set; }

    /// <summary>Totaluri istorice REALE, introduse manual (evidența dinainte de aplicație nu se
    /// reintroduce rând-cu-rând) — NU calculate din rândurile de Booking.</summary>
    public int? LifetimeStayDays { get; set; }
    public int? LifetimeBookingsCompleted { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<CleaningAssignment> CleaningAssignments { get; set; } = new List<CleaningAssignment>();
    public ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();
}
