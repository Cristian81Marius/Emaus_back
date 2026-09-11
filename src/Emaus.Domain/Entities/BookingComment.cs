namespace Emaus.Domain.Entities;

/// <summary>Un comentariu/observație pe o solicitare — firul de discuție al nucleului
/// înainte de decizie (ex. "verificăm dacă a mai fost blocat"), păstrat ca istoric,
/// nu pierdut într-un chat extern.</summary>
public class BookingComment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid AuthorUserId { get; set; }
    public ApplicationUser AuthorUser { get; set; } = null!;

    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
