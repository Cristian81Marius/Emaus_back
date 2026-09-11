namespace Emaus.Domain.Entities;

/// <summary>Înscrierea unui voluntar la o oportunitate — join table simplu, dar cu timestamp
/// propriu ca să poată sta la baza unui viitor contor de "ore de voluntariat".</summary>
public class OpportunitySignup
{
    public Guid Id { get; set; }

    public Guid OpportunityId { get; set; }
    public VolunteerOpportunity Opportunity { get; set; } = null!;

    public Guid VolunteerUserId { get; set; }
    public ApplicationUser VolunteerUser { get; set; } = null!;

    public DateTime SignedUpAt { get; set; } = DateTime.UtcNow;
}
