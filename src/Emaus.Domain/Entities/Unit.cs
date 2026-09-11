namespace Emaus.Domain.Entities;

/// <summary>Unitatea efectiv cazabilă — un apartament întreg, sau o cameră dintr-un apartament
/// comun (ex. "Ap 124", "Dormitor", "Sufragerie"). Statusul curent trăiește aici, nu pe Property,
/// pentru că două unități din aceeași adresă pot avea statusuri diferite în același timp.</summary>
public class Unit
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    /// <summary>Numele scurt afișat, ex. "Ap 94" sau "Dormitor". Pentru o adresă cu o singură
    /// unitate, poate coincide cu adresa scurtată.</summary>
    public required string Name { get; set; }
    public int Capacity { get; set; } = 1;

    public UnitStatus Status { get; set; } = UnitStatus.Available;
    public string? StatusNotes { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<CleaningTask> CleaningTasks { get; set; } = new List<CleaningTask>();
}
