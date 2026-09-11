using Emaus.Api.Common;
using Emaus.Api.Dtos.Properties;
using Emaus.Api.Services.Notifications;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Properties;

/// <summary>Locațiile Emaus — o proprietate (adresă) conține 1+ unități cazabile, vezi
/// schema.md pentru raționamentul din spatele acestei structuri pe două niveluri. Serviciul
/// ține și logica pentru unități (PATCH status, disponibile), nu doar pentru proprietăți —
/// nu merită un al doilea serviciu pentru o entitate atât de mică.</summary>
public class PropertyService(
    IRepository<Property> properties,
    IRepository<Unit> units,
    IRepository<CleaningAssignment> cleaningAssignments,
    IUnitOfWork unitOfWork,
    NotificationService notifications)
{
    public async Task<List<PropertyDto>> GetAllAsync()
    {
        var list = await properties.Query().Include(p => p.Units).OrderBy(p => p.Address).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<PropertyDto>> GetByIdAsync(Guid id)
    {
        var property = await properties.Query().Include(p => p.Units).SingleOrDefaultAsync(p => p.Id == id);
        return property is null
            ? ServiceResult<PropertyDto>.NotFound("Locația nu există.")
            : ServiceResult<PropertyDto>.Ok(ToDto(property));
    }

    public async Task<ServiceResult<PropertyDto>> CreateAsync(CreatePropertyRequest request)
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Address = request.Address,
            ShortLabel = request.ShortLabel,
            IsTemporary = request.IsTemporary,
            Notes = request.Notes,
            Interfon = request.Interfon,
            KeyHolders = request.KeyHolders ?? new List<string>(),
            KeyNotes = request.KeyNotes,
            LifetimeStayDays = request.LifetimeStayDays,
            LifetimeBookingsCompleted = request.LifetimeBookingsCompleted
        };
        properties.Add(property);
        await unitOfWork.SaveChangesAsync();
        return ServiceResult<PropertyDto>.Ok(ToDto(property));
    }

    /// <summary>Editează identitatea locației — NU statusul unităților (asta e UpdateUnitStatusAsync).</summary>
    public async Task<ServiceResult<PropertyDto>> UpdateAsync(Guid id, UpdatePropertyRequest request)
    {
        var property = await properties.Query().Include(p => p.Units).SingleOrDefaultAsync(p => p.Id == id);
        if (property is null) return ServiceResult<PropertyDto>.NotFound("Locația nu există.");

        if (request.Address is not null) property.Address = request.Address;
        if (request.ShortLabel is not null) property.ShortLabel = request.ShortLabel;
        if (request.Notes is not null) property.Notes = request.Notes;
        if (request.Interfon is not null) property.Interfon = request.Interfon;
        if (request.KeyHolders is not null) property.KeyHolders = request.KeyHolders;
        if (request.KeyNotes is not null) property.KeyNotes = request.KeyNotes;
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<PropertyDto>.Ok(ToDto(property));
    }

    public async Task<ServiceResult<UnitDto>> AddUnitAsync(Guid propertyId, CreateUnitRequest request)
    {
        var property = await properties.GetByIdAsync(propertyId);
        if (property is null) return ServiceResult<UnitDto>.NotFound("Locația nu există.");

        var unit = new Unit { Id = Guid.NewGuid(), PropertyId = propertyId, Name = request.Name, Capacity = request.Capacity };
        units.Add(unit);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<UnitDto>.Ok(ToUnitDto(unit));
    }

    /// <summary>Dacă noul status e NeedsCleaning și înainte NU era (evită retrimitere la fiecare
    /// salvare), notifică voluntarii din rotația de curățenie a locației — vezi docs/API.md §4.</summary>
    public async Task<ServiceResult<UnitDto>> UpdateUnitStatusAsync(Guid unitId, UpdateUnitStatusRequest request)
    {
        var unit = await units.Query().Include(u => u.Property).SingleOrDefaultAsync(u => u.Id == unitId);
        if (unit is null) return ServiceResult<UnitDto>.NotFound("Unitatea nu există.");

        var wasNeedsCleaning = unit.Status == UnitStatus.NeedsCleaning;
        unit.Status = request.Status;
        unit.StatusNotes = request.StatusNotes;
        await unitOfWork.SaveChangesAsync();

        if (request.Status == UnitStatus.NeedsCleaning && !wasNeedsCleaning)
        {
            var volunteerIds = await cleaningAssignments.Query()
                .Where(a => a.PropertyId == unit.PropertyId)
                .Select(a => a.VolunteerUserId)
                .Distinct()
                .ToListAsync();

            if (volunteerIds.Count > 0)
            {
                await notifications.NotifyUsersAsync(volunteerIds, NotificationType.UnitNeedsCleaning,
                    $"{unit.Name} ({unit.Property.ShortLabel}) necesită curățenie.", nameof(Unit), unit.Id);
            }
        }

        return ServiceResult<UnitDto>.Ok(ToUnitDto(unit));
    }

    public async Task<List<UnitDto>> GetAvailableUnitsAsync()
    {
        var list = await units.Query().Where(u => u.Status == UnitStatus.Available).OrderBy(u => u.Name).ToListAsync();
        return list.Select(ToUnitDto).ToList();
    }

    private static PropertyDto ToDto(Property p) => new(
        p.Id, p.Address, p.ShortLabel, p.IsTemporary, p.Notes, p.Interfon, p.KeyHolders, p.KeyNotes,
        p.LifetimeStayDays, p.LifetimeBookingsCompleted, p.Units.Select(ToUnitDto).ToList());

    private static UnitDto ToUnitDto(Unit u) =>
        new(u.Id, u.PropertyId, u.Name, u.Capacity, u.Status, u.StatusNotes);
}
