using Emaus.Api.Dtos.Beneficiaries;
using Emaus.Api.Services.Beneficiaries;
using Emaus.Api.Tests.TestDoubles;
using Emaus.Domain;
using Emaus.Domain.Entities;

namespace Emaus.Api.Tests.Services;

public class BeneficiaryServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _testDb = new();
    private readonly BeneficiaryService _sut;

    public BeneficiaryServiceTests()
    {
        _sut = new BeneficiaryService(_testDb.Repo<Beneficiary>(), _testDb.UnitOfWork());
    }

    public void Dispose() => _testDb.Dispose();

    private async Task<Beneficiary> AddBeneficiaryAsync(string fullName, string? phone = null, DateTime? createdAt = null)
    {
        var beneficiary = new Beneficiary { Id = Guid.NewGuid(), FullName = fullName, Phone = phone };
        if (createdAt is not null) beneficiary.CreatedAt = createdAt.Value;
        _testDb.Db.Beneficiaries.Add(beneficiary);
        await _testDb.Db.SaveChangesAsync();
        return beneficiary;
    }

    [Fact]
    public async Task GetAllAsync_SearchIgnoresDiacritics_OnFullName()
    {
        await AddBeneficiaryAsync("Șerban Emilia");
        await AddBeneficiaryAsync("Popescu Ana");

        var result = await _sut.GetAllAsync(search: "Serban", page: 1, pageSize: 30);

        var item = Assert.Single(result.Items);
        Assert.Equal("Șerban Emilia", item.FullName);
    }

    [Fact]
    public async Task GetAllAsync_SearchMatchesPhoneRawContains()
    {
        await AddBeneficiaryAsync("Cotulbea Marian", phone: "0766884441");
        await AddBeneficiaryAsync("Alt Beneficiar", phone: "0700000000");

        var result = await _sut.GetAllAsync(search: "6688", page: 1, pageSize: 30);

        Assert.Single(result.Items);
        Assert.Equal("Cotulbea Marian", result.Items[0].FullName);
    }

    [Fact]
    public async Task GetAllAsync_NoSearch_OrdersByCreatedAtDescending()
    {
        await AddBeneficiaryAsync("Primul", createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddBeneficiaryAsync("Ultimul", createdAt: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _sut.GetAllAsync(search: null, page: 1, pageSize: 30);

        Assert.Equal("Ultimul", result.Items[0].FullName);
        Assert.Equal("Primul", result.Items[1].FullName);
    }

    [Fact]
    public async Task GetAllAsync_RespectsPaginationAndHasMore()
    {
        for (var i = 0; i < 5; i++) await AddBeneficiaryAsync($"Beneficiar {i}");

        var page1 = await _sut.GetAllAsync(search: null, page: 1, pageSize: 2);
        var page3 = await _sut.GetAllAsync(search: null, page: 3, pageSize: 2);

        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.Equal(5, page1.Total);
        Assert.Single(page3.Items);
        Assert.False(page3.HasMore);
    }

    [Fact]
    public async Task BlockAsync_EmptyReason_DefaultsToNespecificat()
    {
        var beneficiary = await AddBeneficiaryAsync("De blocat");

        var result = await _sut.BlockAsync(beneficiary.Id, new BlockBeneficiaryRequest(""));

        Assert.True(result.IsSuccess);
        var reloaded = await _testDb.Db.Beneficiaries.FindAsync(beneficiary.Id);
        Assert.Equal("Nespecificat", reloaded!.BlockedReason);
        Assert.Equal(BeneficiaryStatus.Blocked, reloaded.Status);
    }

    [Fact]
    public async Task UnblockAsync_ClearsStatusAndReason()
    {
        var beneficiary = await AddBeneficiaryAsync("De deblocat");
        await _sut.BlockAsync(beneficiary.Id, new BlockBeneficiaryRequest("motiv"));

        var result = await _sut.UnblockAsync(beneficiary.Id);

        Assert.True(result.IsSuccess);
        var reloaded = await _testDb.Db.Beneficiaries.FindAsync(beneficiary.Id);
        Assert.Equal(BeneficiaryStatus.Active, reloaded!.Status);
        Assert.Null(reloaded.BlockedReason);
    }

    [Fact]
    public async Task UpdateAsync_OnlyChangesProvidedFields()
    {
        var beneficiary = await AddBeneficiaryAsync("Nume Vechi", phone: "0700000001");

        var result = await _sut.UpdateAsync(beneficiary.Id, new UpdateBeneficiaryRequest(
            FullName: "Nume Nou", Phone: null, LocalityId: null, LocalityFreeText: null, Address: null,
            IdCardSeries: null, IdCardNumber: null, SupportPersonName: null, SupportPersonPhone: null,
            Age: null, MaterialSituation: null, ReferralSource: null, Notes: null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Nume Nou", result.Value!.FullName);
        Assert.Equal("0700000001", result.Value.Phone);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateBeneficiaryRequest(
            "x", null, null, null, null, null, null, null, null, null, null, null, null));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetAllAsync_PropertyIdFilter_OnlyReturnsBeneficiariesWithBookingAtThatProperty()
    {
        var stayedHere = await AddBeneficiaryAsync("A stat aici");
        var stayedElsewhere = await AddBeneficiaryAsync("A stat altundeva");
        var neverStayed = await AddBeneficiaryAsync("N-a fost cazat niciodată");

        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Tester", Phone = "0700000009", PasswordHash = "x" };
        var propertyHere = new Property { Id = Guid.NewGuid(), Address = "Aici", ShortLabel = "Aici" };
        var propertyElsewhere = new Property { Id = Guid.NewGuid(), Address = "Altundeva", ShortLabel = "Altundeva" };
        var unitHere = new Unit { Id = Guid.NewGuid(), PropertyId = propertyHere.Id, Name = "U1", Capacity = 1 };
        var unitElsewhere = new Unit { Id = Guid.NewGuid(), PropertyId = propertyElsewhere.Id, Name = "U2", Capacity = 1 };
        _testDb.Db.Users.Add(user);
        _testDb.Db.Properties.AddRange(propertyHere, propertyElsewhere);
        _testDb.Db.Units.AddRange(unitHere, unitElsewhere);
        _testDb.Db.Bookings.AddRange(
            new Booking
            {
                Id = Guid.NewGuid(), BeneficiaryId = stayedHere.Id, UnitId = unitHere.Id,
                RequestedCheckIn = new DateOnly(2026, 1, 1), RequestedCheckOut = new DateOnly(2026, 1, 10),
                Status = BookingStatus.Completed, CreatedByUserId = user.Id,
            },
            new Booking
            {
                Id = Guid.NewGuid(), BeneficiaryId = stayedElsewhere.Id, UnitId = unitElsewhere.Id,
                RequestedCheckIn = new DateOnly(2026, 1, 1), RequestedCheckOut = new DateOnly(2026, 1, 10),
                Status = BookingStatus.Completed, CreatedByUserId = user.Id,
            });
        await _testDb.Db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(search: null, page: 1, pageSize: 30, propertyId: propertyHere.Id);

        var item = Assert.Single(result.Items);
        Assert.Equal(stayedHere.Id, item.Id);
        Assert.DoesNotContain(result.Items, b => b.Id == stayedElsewhere.Id || b.Id == neverStayed.Id);
    }

    [Fact]
    public async Task UpdateAsync_SetsContractFields()
    {
        var beneficiary = await AddBeneficiaryAsync("Contract Test");

        var result = await _sut.UpdateAsync(beneficiary.Id, new UpdateBeneficiaryRequest(
            FullName: null, Phone: null, LocalityId: null, LocalityFreeText: null,
            Address: "Str. Exemplu nr. 1", IdCardSeries: "ZD", IdCardNumber: "073611",
            SupportPersonName: "Ion Popescu", SupportPersonPhone: "0711112222",
            Age: null, MaterialSituation: null, ReferralSource: null, Notes: null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Str. Exemplu nr. 1", result.Value!.Address);
        Assert.Equal("ZD", result.Value.IdCardSeries);
        Assert.Equal("073611", result.Value.IdCardNumber);
        Assert.Equal("Ion Popescu", result.Value.SupportPersonName);
        Assert.Equal("0711112222", result.Value.SupportPersonPhone);
    }
}
