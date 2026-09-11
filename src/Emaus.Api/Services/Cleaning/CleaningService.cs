using Emaus.Api.Common;
using Emaus.Api.Dtos.Cleaning;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Cleaning;

/// <summary>Curățenie & chei — vezi secțiunea cu același nume din plan. Turele apar automat
/// la check-out (BookingService.CheckOutAsync); aici trăiește restul: listarea, finalizarea
/// (care repune unitatea "Liber & igienizat") și rotația de voluntari per adresă.</summary>
public class CleaningService(
    IRepository<CleaningTask> cleaningTasks,
    IRepository<CleaningAssignment> cleaningAssignments,
    IRepository<Property> properties,
    IRepository<ApplicationUser> users,
    IUnitOfWork unitOfWork)
{
    public async Task<List<CleaningTaskDto>> GetTasksAsync(bool onlyPending)
    {
        var query = cleaningTasks.Query()
            .Include(t => t.Unit).ThenInclude(u => u.Property)
            .Include(t => t.CompletedByUser)
            .AsQueryable();

        if (onlyPending) query = query.Where(t => t.Status != CleaningTaskStatus.Done);

        var list = await query.OrderBy(t => t.ScheduledDate).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    /// <summary>Marchează tura ca finalizată — unitatea revine automat "Liber & igienizat".</summary>
    public async Task<ServiceResult<CleaningTaskDto>> CompleteAsync(Guid id, CompleteCleaningTaskRequest request, Guid completedByUserId)
    {
        var task = await cleaningTasks.Query().Include(t => t.Unit).ThenInclude(u => u.Property)
            .SingleOrDefaultAsync(t => t.Id == id);
        if (task is null) return ServiceResult<CleaningTaskDto>.NotFound("Tura de curățenie nu există.");

        task.Status = CleaningTaskStatus.Done;
        task.CompletedByUserId = completedByUserId;
        task.CompletedAt = DateTime.UtcNow;
        task.Notes = request.Notes;
        task.Unit.Status = UnitStatus.Available;
        await unitOfWork.SaveChangesAsync();

        var completedBy = await users.GetByIdAsync(completedByUserId);
        task.CompletedByUser = completedBy;
        return ServiceResult<CleaningTaskDto>.Ok(ToDto(task));
    }

    public async Task<List<CleaningAssignmentDto>> GetAssignmentsAsync(Guid? propertyId)
    {
        var query = cleaningAssignments.Query().Include(a => a.Property).Include(a => a.VolunteerUser).AsQueryable();
        if (propertyId is not null) query = query.Where(a => a.PropertyId == propertyId);

        var list = await query.ToListAsync();
        return list.Select(a => new CleaningAssignmentDto(
            a.Id, a.PropertyId, a.Property.Address, a.VolunteerUserId, a.VolunteerUser.FullName, a.ScheduledDayOfWeek)).ToList();
    }

    /// <summary>Adaugă un voluntar în rotația de curățenie a unei adrese — echivalentul
    /// "Responsabil 1..4" din evidența actuală.</summary>
    public async Task<ServiceResult<CleaningAssignmentDto>> CreateAssignmentAsync(CreateCleaningAssignmentRequest request)
    {
        var property = await properties.GetByIdAsync(request.PropertyId);
        if (property is null) return ServiceResult<CleaningAssignmentDto>.NotFound("Proprietatea nu există.");

        var volunteer = await users.GetByIdAsync(request.VolunteerUserId);
        if (volunteer is null) return ServiceResult<CleaningAssignmentDto>.NotFound("Voluntarul nu există.");

        var assignment = new CleaningAssignment
        {
            Id = Guid.NewGuid(),
            PropertyId = request.PropertyId,
            VolunteerUserId = request.VolunteerUserId,
            ScheduledDayOfWeek = request.ScheduledDayOfWeek
        };
        cleaningAssignments.Add(assignment);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<CleaningAssignmentDto>.Ok(new CleaningAssignmentDto(
            assignment.Id, property.Id, property.Address, volunteer.Id, volunteer.FullName, assignment.ScheduledDayOfWeek));
    }

    private static CleaningTaskDto ToDto(CleaningTask t) => new(
        t.Id, t.UnitId, t.Unit.Name, t.Unit.Property.Address, t.ScheduledDate,
        t.Status, t.CompletedByUser?.FullName, t.CompletedAt, t.Notes);
}
