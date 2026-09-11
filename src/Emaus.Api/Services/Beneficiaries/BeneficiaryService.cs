using System.Globalization;
using System.Text;
using Emaus.Api.Common;
using Emaus.Api.Dtos.Beneficiaries;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Beneficiaries;

/// <summary>Persoanele găzduite. Vezi entitatea Beneficiary pentru raționamentul câmpurilor —
/// aici trăiește logica de căutare și de blocare/deblocare (cazul "Blocat din 15 septembrie!!"
/// din evidența actuală, dar cu motivul păstrat explicit).</summary>
public class BeneficiaryService(IRepository<Beneficiary> beneficiaries, IUnitOfWork unitOfWork)
{
    /// <summary>Paginat, "recenți întâi" implicit. Căutarea ignoră diacriticele pe nume
    /// (normalizează AMBELE părți — vezi NormalizeSearchText) și se aplică simplu (Contains)
    /// pe telefon. SQLite nu poate traduce normalizarea NFD în SQL, deci filtrarea pe nume
    /// se face în memorie — acceptabil la scara unei asociații (sute, nu milioane, de
    /// beneficiari); telefonul rămâne filtrabil direct în interogare.</summary>
    public async Task<PagedResult<BeneficiaryDto>> GetAllAsync(string? search, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 30 : pageSize;

        var query = beneficiaries.Query().Include(b => b.Locality).OrderByDescending(b => b.CreatedAt).AsQueryable();

        if (string.IsNullOrWhiteSpace(search))
        {
            var total = await query.CountAsync();
            var page1 = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<BeneficiaryDto>(page1.Select(ToDto).ToList(), page, pageSize, total, page * pageSize < total);
        }

        var needle = NormalizeSearchText(search);
        var all = await query.ToListAsync();
        var filtered = all
            .Where(b => NormalizeSearchText(b.FullName).Contains(needle) || (b.Phone is not null && b.Phone.Contains(search)))
            .ToList();

        var pageItems = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<BeneficiaryDto>(
            pageItems.Select(ToDto).ToList(), page, pageSize, filtered.Count, page * pageSize < filtered.Count);
    }

    public async Task<ServiceResult<BeneficiaryDto>> GetByIdAsync(Guid id)
    {
        var beneficiary = await beneficiaries.Query().Include(b => b.Locality).SingleOrDefaultAsync(b => b.Id == id);
        return beneficiary is null
            ? ServiceResult<BeneficiaryDto>.NotFound("Beneficiarul nu există.")
            : ServiceResult<BeneficiaryDto>.Ok(ToDto(beneficiary));
    }

    public async Task<ServiceResult<BeneficiaryDto>> CreateAsync(CreateBeneficiaryRequest request)
    {
        var beneficiary = new Beneficiary
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            LocalityId = request.LocalityId,
            LocalityFreeText = request.LocalityFreeText,
            Address = request.Address,
            IdCardSeries = request.IdCardSeries,
            IdCardNumber = request.IdCardNumber,
            SupportPersonName = request.SupportPersonName,
            SupportPersonPhone = request.SupportPersonPhone,
            Age = request.Age,
            MaterialSituation = request.MaterialSituation,
            ReferralSource = request.ReferralSource,
            Notes = request.Notes
        };
        beneficiaries.Add(beneficiary);
        await unitOfWork.SaveChangesAsync();

        var reloaded = await beneficiaries.Query().Include(b => b.Locality).SingleAsync(b => b.Id == beneficiary.Id);
        return ServiceResult<BeneficiaryDto>.Ok(ToDto(reloaded));
    }

    public async Task<ServiceResult<BeneficiaryDto>> UpdateAsync(Guid id, UpdateBeneficiaryRequest request)
    {
        var beneficiary = await beneficiaries.Query().Include(b => b.Locality).SingleOrDefaultAsync(b => b.Id == id);
        if (beneficiary is null) return ServiceResult<BeneficiaryDto>.NotFound("Beneficiarul nu există.");

        if (request.FullName is not null) beneficiary.FullName = request.FullName;
        if (request.Phone is not null) beneficiary.Phone = request.Phone;
        if (request.LocalityId is not null) beneficiary.LocalityId = request.LocalityId;
        if (request.LocalityFreeText is not null) beneficiary.LocalityFreeText = request.LocalityFreeText;
        if (request.Address is not null) beneficiary.Address = request.Address;
        if (request.IdCardSeries is not null) beneficiary.IdCardSeries = request.IdCardSeries;
        if (request.IdCardNumber is not null) beneficiary.IdCardNumber = request.IdCardNumber;
        if (request.SupportPersonName is not null) beneficiary.SupportPersonName = request.SupportPersonName;
        if (request.SupportPersonPhone is not null) beneficiary.SupportPersonPhone = request.SupportPersonPhone;
        if (request.Age is not null) beneficiary.Age = request.Age;
        if (request.MaterialSituation is not null) beneficiary.MaterialSituation = request.MaterialSituation;
        if (request.ReferralSource is not null) beneficiary.ReferralSource = request.ReferralSource;
        if (request.Notes is not null) beneficiary.Notes = request.Notes;
        await unitOfWork.SaveChangesAsync();

        var reloaded = await beneficiaries.Query().Include(b => b.Locality).SingleAsync(b => b.Id == id);
        return ServiceResult<BeneficiaryDto>.Ok(ToDto(reloaded));
    }

    public async Task<ServiceResult> BlockAsync(Guid id, BlockBeneficiaryRequest request)
    {
        var beneficiary = await beneficiaries.GetByIdAsync(id);
        if (beneficiary is null) return ServiceResult.NotFound("Beneficiarul nu există.");

        beneficiary.Status = BeneficiaryStatus.Blocked;
        beneficiary.BlockedReason = string.IsNullOrWhiteSpace(request.Reason) ? "Nespecificat" : request.Reason;
        beneficiary.BlockedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnblockAsync(Guid id)
    {
        var beneficiary = await beneficiaries.GetByIdAsync(id);
        if (beneficiary is null) return ServiceResult.NotFound("Beneficiarul nu există.");

        beneficiary.Status = BeneficiaryStatus.Active;
        beneficiary.BlockedReason = null;
        beneficiary.BlockedAt = null;
        await unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    /// <summary>NFD descompune fiecare literă cu diacritice în literă de bază + semn separat,
    /// apoi eliminăm semnele (categoria Unicode NonSpacingMark) — "atsi" găsește "Ștefănescu".</summary>
    private static string NormalizeSearchText(string s)
    {
        var decomposed = s.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
        }
        return builder.ToString().ToLowerInvariant();
    }

    private static BeneficiaryDto ToDto(Beneficiary b) => new(
        b.Id, b.FullName, b.Phone, b.LocalityId, b.Locality?.Name, b.LocalityFreeText,
        b.Address, b.IdCardSeries, b.IdCardNumber, b.SupportPersonName, b.SupportPersonPhone,
        b.Age, b.MaterialSituation, b.ReferralSource, b.Status, b.BlockedReason, b.AccountBalance, b.Notes);
}
