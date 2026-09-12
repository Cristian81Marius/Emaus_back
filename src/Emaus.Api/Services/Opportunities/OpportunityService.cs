using Emaus.Api.Common;
using Emaus.Api.Dtos.Opportunities;
using Emaus.Api.Services.Notifications;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Opportunities;

/// <summary>Curățenie, evenimente, vizite, promovare — calendarul unic de oportunități
/// de implicare descris în plan, cu înscriere directă.</summary>
public class OpportunityService(
    IRepository<VolunteerOpportunity> opportunities,
    IRepository<OpportunitySignup> signups,
    IUnitOfWork unitOfWork,
    NotificationService notifications)
{
    /// <summary>Sortate după scheduledAt crescător — cele fără dată stabilită (null) coboară
    /// la coada listei, nu la început (DateTime? implicit sortează null primul).</summary>
    public async Task<List<OpportunityDto>> GetAllAsync(Guid currentUserId)
    {
        var list = await BaseQuery().ToListAsync();
        return list
            .OrderBy(o => o.ScheduledAt is null)
            .ThenBy(o => o.ScheduledAt)
            .Select(o => ToDto(o, currentUserId))
            .ToList();
    }

    public async Task<ServiceResult<OpportunityDto>> GetByIdAsync(Guid id, Guid currentUserId)
    {
        var opportunity = await BaseQuery().SingleOrDefaultAsync(o => o.Id == id);
        return opportunity is null
            ? ServiceResult<OpportunityDto>.NotFound("Oportunitatea nu există.")
            : ServiceResult<OpportunityDto>.Ok(ToDto(opportunity, currentUserId));
    }

    public async Task<OpportunityDto> CreateAsync(CreateOpportunityRequest request, Guid currentUserId)
    {
        var opportunity = new VolunteerOpportunity
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Title = request.Title,
            Description = request.Description,
            ScheduledAt = request.ScheduledAt,
            HasTime = request.HasTime,
            PropertyId = request.PropertyId,
            Capacity = request.Capacity
        };
        opportunities.Add(opportunity);
        await unitOfWork.SaveChangesAsync();

        if (request.NotifyEveryone)
        {
            var when = request.ScheduledAt is { } scheduledAt ? $" ({scheduledAt:dd.MM HH:mm})" : "";
            await notifications.NotifyRoleAsync(UserRole.Volunteer, NotificationType.NewOpportunityPublished,
                $"Oportunitate nouă: {opportunity.Title}{when}", nameof(VolunteerOpportunity), opportunity.Id);
        }

        var created = await BaseQuery().SingleAsync(o => o.Id == opportunity.Id);
        return ToDto(created, currentUserId);
    }

    /// <summary>Nu retrimite nicio notificare — spre deosebire de creare.</summary>
    public async Task<ServiceResult<OpportunityDto>> UpdateAsync(Guid id, UpdateOpportunityRequest request, Guid currentUserId)
    {
        var opportunity = await opportunities.GetByIdAsync(id);
        if (opportunity is null) return ServiceResult<OpportunityDto>.NotFound("Oportunitatea nu există.");

        opportunity.Type = request.Type;
        opportunity.Title = request.Title;
        opportunity.Description = request.Description;
        opportunity.ScheduledAt = request.ScheduledAt;
        opportunity.HasTime = request.HasTime;
        opportunity.PropertyId = request.PropertyId;
        opportunity.Capacity = request.Capacity;
        await unitOfWork.SaveChangesAsync();

        var updated = await BaseQuery().SingleAsync(o => o.Id == id);
        return ServiceResult<OpportunityDto>.Ok(ToDto(updated, currentUserId));
    }

    public async Task<ServiceResult> SignUpAsync(Guid id, Guid userId)
    {
        var opportunity = await opportunities.Query().Include(o => o.Signups).SingleOrDefaultAsync(o => o.Id == id);
        if (opportunity is null) return ServiceResult.NotFound("Oportunitatea nu există.");

        if (opportunity.Signups.Any(s => s.VolunteerUserId == userId)) return ServiceResult.Ok();

        if (opportunity.Capacity is not null && opportunity.Signups.Count >= opportunity.Capacity)
        {
            return ServiceResult.Conflict("Nu mai sunt locuri disponibile.");
        }

        signups.Add(new OpportunitySignup { Id = Guid.NewGuid(), OpportunityId = id, VolunteerUserId = userId });
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelSignUpAsync(Guid id, Guid userId)
    {
        var signup = await signups.Query().SingleOrDefaultAsync(s => s.OpportunityId == id && s.VolunteerUserId == userId);
        if (signup is null) return ServiceResult.NotFound("Nu ești înscris la această oportunitate.");

        signups.Remove(signup);
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private IQueryable<VolunteerOpportunity> BaseQuery() =>
        opportunities.Query().Include(o => o.Property).Include(o => o.Signups).ThenInclude(s => s.VolunteerUser);

    private static OpportunityDto ToDto(VolunteerOpportunity o, Guid currentUserId) => new(
        o.Id, o.Type, o.Title, o.Description, o.ScheduledAt, o.HasTime, o.PropertyId, o.Property?.Address,
        o.Capacity, o.Signups.Count, o.Signups.Any(s => s.VolunteerUserId == currentUserId),
        o.Signups.Select(s => s.VolunteerUser.FullName).ToList());
}
