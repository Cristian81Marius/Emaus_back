namespace Emaus.Domain.Entities;

/// <summary>O notificare pentru un utilizator, legată generic de orice entitate
/// (solicitare, tură de curățenie, sesizare) prin RelatedEntityType/RelatedEntityId,
/// ca să nu fie nevoie de un tabel de notificări separat per tip de eveniment.</summary>
public class Notification
{
    public Guid Id { get; set; }

    public Guid RecipientUserId { get; set; }
    public ApplicationUser RecipientUser { get; set; } = null!;

    public NotificationType Type { get; set; }
    public required string Message { get; set; }

    /// <summary>Ex. "Booking", "MaintenanceTicket" — folosit de client ca să știe ce ecran
    /// să deschidă la apăsarea notificării.</summary>
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
