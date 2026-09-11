using Emaus.Api.Dtos.Bob;
using Emaus.Api.Services.Bob;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain.Bob;

namespace Emaus.Api.Tests.Services;

public class BobBoxServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly BobBoxService _sut;
    private readonly BoxCategory _category;

    public BobBoxServiceTests()
    {
        _category = new BoxCategory { Id = Guid.NewGuid(), Key = "bacanie", Title = "Băcănie", SortOrder = 0 };
        _testDb.Db.BoxCategories.Add(_category);
        _testDb.Db.BoxItems.Add(new BoxItem { Id = Guid.NewGuid(), CategoryId = _category.Id, Name = "Făină", Price = 1.69m, Checked = true, SortOrder = 0 });
        _testDb.Db.BoxItems.Add(new BoxItem { Id = Guid.NewGuid(), CategoryId = _category.Id, Name = "Zahăr", Price = 3.79m, Checked = false, SortOrder = 1 });
        _testDb.Db.SaveChanges();

        _sut = new BobBoxService(_testDb.Repo<BoxCategory>(), _testDb.Repo<BoxItem>(), _testDb.UnitOfWork());
    }

    public void Dispose() => _testDb.Dispose();

    [Fact]
    public async Task GetCategoriesAsync_ReturnsItemsInSortOrder()
    {
        var categories = await _sut.GetCategoriesAsync();

        var category = Assert.Single(categories);
        Assert.Equal("Făină", category.Items[0].Name);
        Assert.Equal("Zahăr", category.Items[1].Name);
    }

    [Fact]
    public async Task CreateItemAsync_UnknownCategory_ReturnsNotFound()
    {
        var result = await _sut.CreateItemAsync(new CreateBoxItemRequest("categorie-inexistenta", "Orez", 5.89m));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateItemAsync_DefaultsCheckedTrueAndAppendsAtEnd()
    {
        var result = await _sut.CreateItemAsync(new CreateBoxItemRequest("bacanie", "Orez", 5.89m));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Checked);

        var categories = await _sut.GetCategoriesAsync();
        Assert.Equal("Orez", categories.Single().Items[^1].Name);
    }

    [Fact]
    public async Task UpdateItemAsync_ChecksOnlyProvidedField()
    {
        var item = _testDb.Db.BoxItems.First(i => i.Name == "Zahăr");

        var result = await _sut.UpdateItemAsync(item.Id, new UpdateBoxItemRequest(Checked: true, Price: null, Name: null));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Checked);
        Assert.Equal(3.79m, result.Value.Price);
    }

    [Fact]
    public async Task UpdateItemAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.UpdateItemAsync(Guid.NewGuid(), new UpdateBoxItemRequest(true, null, null));

        Assert.False(result.IsSuccess);
    }
}
