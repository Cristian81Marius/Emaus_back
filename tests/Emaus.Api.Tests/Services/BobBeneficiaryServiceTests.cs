using Emaus.Api.Dtos.Bob;
using Emaus.Api.Services.Bob;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Bob;

namespace Emaus.Api.Tests.Services;

public class BobBeneficiaryServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly BobBeneficiaryService _sut;

    public BobBeneficiaryServiceTests()
    {
        _sut = new BobBeneficiaryService(_testDb.Repo<BobBeneficiary>(), _testDb.UnitOfWork());
    }

    public void Dispose() => _testDb.Dispose();

    [Fact]
    public async Task CreateAsync_DefaultsToActiveAndNotContactedNotDelivered()
    {
        var dto = await _sut.CreateAsync(new CreateBobBeneficiaryRequest("Vintilă Alexandra", null, null, null, "Sami", null));

        Assert.Equal(BobBeneficiaryStatus.Active, dto.Status);
        Assert.False(dto.Contacted);
        Assert.False(dto.Delivered);
    }

    [Fact]
    public async Task UpdateAsync_TogglesOnlyOneFieldAtATime()
    {
        var created = await _sut.CreateAsync(new CreateBobBeneficiaryRequest("Test", null, null, null, "Sami", null));

        var afterContacted = await _sut.UpdateAsync(created.Id, new UpdateBobBeneficiaryRequest(
            null, null, null, null, null, null, Contacted: true, Delivered: null, Notes: null));

        Assert.True(afterContacted.IsSuccess);
        Assert.True(afterContacted.Value!.Contacted);
        Assert.False(afterContacted.Value.Delivered);
        Assert.Equal("Sami", afterContacted.Value.AssignedVolunteerName);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateBobBeneficiaryRequest(
            null, null, null, null, null, null, null, null, null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByFullName()
    {
        await _sut.CreateAsync(new CreateBobBeneficiaryRequest("Zetescu", null, null, null, null, null));
        await _sut.CreateAsync(new CreateBobBeneficiaryRequest("Anescu", null, null, null, null, null));

        var all = await _sut.GetAllAsync();

        Assert.Equal("Anescu", all[0].FullName);
        Assert.Equal("Zetescu", all[1].FullName);
    }
}
