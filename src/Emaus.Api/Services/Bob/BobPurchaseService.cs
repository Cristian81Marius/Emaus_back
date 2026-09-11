using Emaus.Api.Common;
using Emaus.Api.Dtos.Bob;
using Emaus.Domain.Bob;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Bob;

/// <summary>Istoric de cumpărături BOB — SINGURA parte a domeniului care trebuie să persiste
/// real (cerință explicită a utilizatorului, vezi docs/API.md §13.3). Articolele salvate sunt
/// un instantaneu (nume+preț din momentul cumpărăturii), nu o referință live la BoxItem.</summary>
public class BobPurchaseService(IRepository<BoxItem> items, IRepository<BobDeliveryRecord> records, IUnitOfWork unitOfWork)
{
    public async Task<List<BobDeliveryRecordDto>> GetAllAsync()
    {
        var list = await records.Query().Include(r => r.Items).OrderByDescending(r => r.Date).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    /// <summary>Ia TOATE articolele curent Checked din catalog, calculează totalul, salvează un
    /// rând nou persistent. NU golește bifele — runda următoare pornește din aceleași bife.</summary>
    public async Task<ServiceResult<BobDeliveryRecordDto>> CreateAsync(CreatePurchaseRequest request)
    {
        var checkedItems = await items.Query().Where(i => i.Checked).OrderBy(i => i.SortOrder).ToListAsync();
        if (checkedItems.Count == 0) return ServiceResult<BobDeliveryRecordDto>.Invalid("Nu e bifat niciun articol.");

        var record = new BobDeliveryRecord
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Total = checkedItems.Sum(i => i.Price),
            Note = request.Note,
            Items = checkedItems.Select(i => new BobDeliveryRecordItem { Id = Guid.NewGuid(), Name = i.Name, Price = i.Price }).ToList()
        };
        records.Add(record);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<BobDeliveryRecordDto>.Ok(ToDto(record));
    }

    private static BobDeliveryRecordDto ToDto(BobDeliveryRecord r) => new(
        r.Id, r.Date.ToString("yyyy-MM-dd"), r.Items.Select(i => new BobPurchaseItemDto(i.Name, i.Price)).ToList(), r.Total, r.Note);
}
