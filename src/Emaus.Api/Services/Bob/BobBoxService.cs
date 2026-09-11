using Emaus.Api.Common;
using Emaus.Api.Dtos.Bob;
using Emaus.Domain.Bob;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Bob;

/// <summary>Catalogul cutiei BOB — categorii + articole, cu bifa "face parte din runda de
/// cumpărături în lucru ACUM" (`Checked`). Vezi docs/API.md §13.2.</summary>
public class BobBoxService(IRepository<BoxCategory> categories, IRepository<BoxItem> items, IUnitOfWork unitOfWork)
{
    public async Task<List<BoxCategoryDto>> GetCategoriesAsync()
    {
        var list = await categories.Query().Include(c => c.Items).OrderBy(c => c.SortOrder).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    /// <summary>`Price` vine ca `decimal` (JSON number) — nicio parsare de separator zecimal
    /// aici, mobilul normalizează orice virgulă înainte de a trimite (vezi record-ul
    /// `UpdateBoxItemRequest`).</summary>
    public async Task<ServiceResult<BoxItemDto>> UpdateItemAsync(Guid id, UpdateBoxItemRequest request)
    {
        var item = await items.GetByIdAsync(id);
        if (item is null) return ServiceResult<BoxItemDto>.NotFound("Articolul nu există.");

        if (request.Checked is not null) item.Checked = request.Checked.Value;
        if (request.Name is not null) item.Name = request.Name;
        if (request.Price is not null) item.Price = request.Price.Value;
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<BoxItemDto>.Ok(ToDto(item));
    }

    /// <summary>Adaugă un articol nou într-o categorie existentă — Checked: true implicit
    /// (articolul nou intră direct în runda curentă).</summary>
    public async Task<ServiceResult<BoxItemDto>> CreateItemAsync(CreateBoxItemRequest request)
    {
        var category = await categories.Query().SingleOrDefaultAsync(c => c.Key == request.CategoryKey);
        if (category is null) return ServiceResult<BoxItemDto>.NotFound("Categoria nu există.");

        var maxSortOrder = await items.Query().Where(i => i.CategoryId == category.Id)
            .Select(i => (int?)i.SortOrder).MaxAsync() ?? -1;

        var item = new BoxItem
        {
            Id = Guid.NewGuid(), CategoryId = category.Id, Name = request.Name, Price = request.Price,
            Checked = true, SortOrder = maxSortOrder + 1
        };
        items.Add(item);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<BoxItemDto>.Ok(ToDto(item));
    }

    private static BoxCategoryDto ToDto(BoxCategory c) => new(
        c.Key, c.Title, c.Items.OrderBy(i => i.SortOrder).Select(ToDto).ToList());

    private static BoxItemDto ToDto(BoxItem i) => new(i.Id, i.Name, i.Price, i.Checked);
}
