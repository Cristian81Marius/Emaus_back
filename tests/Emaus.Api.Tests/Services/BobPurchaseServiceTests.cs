using Emaus.Api.Dtos.Bob;
using Emaus.Api.Services.Bob;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain.Bob;

namespace Emaus.Api.Tests.Services;

public class BobPurchaseServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly BobPurchaseService _sut;
    private readonly BoxItem _checkedItem;

    public BobPurchaseServiceTests()
    {
        var category = new BoxCategory { Id = Guid.NewGuid(), Key = "bacanie", Title = "Băcănie" };
        _checkedItem = new BoxItem { Id = Guid.NewGuid(), CategoryId = category.Id, Name = "Făină", Price = 1.69m, Checked = true, SortOrder = 0 };
        var uncheckedItem = new BoxItem { Id = Guid.NewGuid(), CategoryId = category.Id, Name = "Sare", Price = 2.75m, Checked = false, SortOrder = 1 };
        _testDb.Db.BoxCategories.Add(category);
        _testDb.Db.BoxItems.AddRange(_checkedItem, uncheckedItem);
        _testDb.Db.SaveChanges();

        _sut = new BobPurchaseService(_testDb.Repo<BoxItem>(), _testDb.Repo<BobDeliveryRecord>(), _testDb.UnitOfWork());
    }

    public void Dispose() => _testDb.Dispose();

    [Fact]
    public async Task CreateAsync_NothingChecked_ReturnsValidationError()
    {
        _checkedItem.Checked = false;
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.CreateAsync(new CreatePurchaseRequest(null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_ComputesTotalFromCheckedItemsOnly()
    {
        var result = await _sut.CreateAsync(new CreatePurchaseRequest("prima rundă"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1.69m, result.Value!.Total);
        Assert.Single(result.Value.Items);
        Assert.Equal("Făină", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task CreateAsync_DoesNotClearCheckedFlags()
    {
        await _sut.CreateAsync(new CreatePurchaseRequest(null));

        var reloaded = await _testDb.Db.BoxItems.FindAsync(_checkedItem.Id);
        Assert.True(reloaded!.Checked);
    }

    [Fact]
    public async Task CreateAsync_SnapshotSurvivesLaterPriceChange()
    {
        var purchase = await _sut.CreateAsync(new CreatePurchaseRequest(null));
        Assert.True(purchase.IsSuccess);

        _checkedItem.Price = 999m;
        await _testDb.Db.SaveChangesAsync();

        var history = await _sut.GetAllAsync();
        Assert.Equal(1.69m, history.Single().Items.Single().Price);
    }
}
