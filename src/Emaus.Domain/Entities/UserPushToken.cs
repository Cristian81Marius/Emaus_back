namespace Emaus.Domain.Entities;

/// <summary>Un token de push (FCM) pentru un dispozitiv anume — un utilizator poate avea mai
/// multe (telefon + tabletă etc.). `Token` e unic la nivel global (nu per utilizator): dacă
/// același dispozitiv se loghează cu alt cont, tokenul se reasignează noului utilizator la
/// următoarea înregistrare, nu rămâne dublat pe ambele conturi.</summary>
public class UserPushToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public required string Token { get; set; }
    /// <summary>"ios" | "android" | "web" — informativ, nu schimbă cum trimitem (FCM abstractizează
    /// diferența), util doar pentru diagnostic (`GET /api/logs` etc.).</summary>
    public string? Platform { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
