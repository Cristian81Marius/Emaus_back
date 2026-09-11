using Emaus.Api.Common;
using Emaus.Api.Dtos.Maintenance;
using Emaus.Api.Services.Notifications;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Maintenance;

/// <summary>Sesizări de mentenanță — de la hârtie igienică la o reparație — puse la
/// dispoziția oricui are timp să le preia. Vezi secțiunea "Cheltuieli & mentenanță" din plan.</summary>
public class MaintenanceService(
    IRepository<MaintenanceTicket> tickets,
    IRepository<Property> properties,
    IRepository<ApplicationUser> users,
    IUnitOfWork unitOfWork,
    NotificationService notifications)
{
    public async Task<List<MaintenanceTicketDto>> GetAllAsync(MaintenanceTicketStatus? status)
    {
        var query = BaseQuery();
        if (status is not null) query = query.Where(t => t.Status == status);

        var list = await query.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<MaintenanceTicketDto>> CreateAsync(CreateMaintenanceTicketRequest request, Guid reportedByUserId)
    {
        var property = await properties.GetByIdAsync(request.PropertyId);
        if (property is null) return ServiceResult<MaintenanceTicketDto>.NotFound("Proprietatea nu există.");

        var ticket = new MaintenanceTicket
        {
            Id = Guid.NewGuid(),
            PropertyId = request.PropertyId,
            Type = request.Type,
            Description = request.Description,
            Priority = request.Priority,
            EstimatedCost = request.EstimatedCost,
            PhotoUrl = request.PhotoUrl,
            ReportedByUserId = reportedByUserId
        };
        tickets.Add(ticket);
        await unitOfWork.SaveChangesAsync();

        await notifications.NotifyRoleAsync(UserRole.Nucleus, NotificationType.NewMaintenanceTicket,
            $"Sesizare nouă la {property.Address}: {request.Description}", nameof(MaintenanceTicket), ticket.Id);

        var created = await BaseQuery().SingleAsync(t => t.Id == ticket.Id);
        return ServiceResult<MaintenanceTicketDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<MaintenanceTicketDto>> AssignAsync(Guid id, AssignTicketRequest request)
    {
        var ticket = await tickets.Query().Include(t => t.Property).SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return ServiceResult<MaintenanceTicketDto>.NotFound("Sesizarea nu există.");

        var assignee = await users.GetByIdAsync(request.AssignedToUserId);
        if (assignee is null) return ServiceResult<MaintenanceTicketDto>.NotFound("Voluntarul nu există.");

        ticket.AssignedToUserId = request.AssignedToUserId;
        ticket.Status = MaintenanceTicketStatus.Assigned;
        await unitOfWork.SaveChangesAsync();

        await notifications.NotifyUserAsync(request.AssignedToUserId, NotificationType.MaintenanceTicketAssigned,
            $"Ți s-a asignat: {ticket.Description} ({ticket.Property.Address})", nameof(MaintenanceTicket), id);

        var updated = await BaseQuery().SingleAsync(t => t.Id == id);
        return ServiceResult<MaintenanceTicketDto>.Ok(ToDto(updated));
    }

    public async Task<ServiceResult<MaintenanceTicketDto>> ResolveAsync(Guid id, ResolveTicketRequest request)
    {
        var ticket = await tickets.GetByIdAsync(id);
        if (ticket is null) return ServiceResult<MaintenanceTicketDto>.NotFound("Sesizarea nu există.");

        ticket.Status = MaintenanceTicketStatus.Resolved;
        ticket.ActualCost = request.ActualCost;
        ticket.ResolvedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();

        var updated = await BaseQuery().SingleAsync(t => t.Id == id);
        return ServiceResult<MaintenanceTicketDto>.Ok(ToDto(updated));
    }

    private IQueryable<MaintenanceTicket> BaseQuery() => tickets.Query()
        .Include(t => t.Property).Include(t => t.ReportedByUser).Include(t => t.AssignedToUser);

    private static MaintenanceTicketDto ToDto(MaintenanceTicket t) => new(
        t.Id, t.PropertyId, t.Property.Address, t.Type, t.Description, t.Priority, t.Status,
        t.EstimatedCost, t.ActualCost, t.PhotoUrl, t.ReportedByUser.FullName, t.CreatedAt,
        t.AssignedToUser?.FullName, t.ResolvedAt);
}
