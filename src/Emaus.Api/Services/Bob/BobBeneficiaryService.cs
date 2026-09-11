using Emaus.Api.Common;
using Emaus.Api.Dtos.Bob;
using Emaus.Domain.Bob;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Services.Bob;

/// <summary>Beneficiari Box of Blessing — entitate separată de Beneficiary (Emaus), vezi
/// docs/API.md §13.1. Set mic (~10-20), fără paginare.</summary>
public class BobBeneficiaryService(IRepository<BobBeneficiary> beneficiaries, IUnitOfWork unitOfWork)
{
    public async Task<List<BobBeneficiaryDto>> GetAllAsync()
    {
        var list = await beneficiaries.Query().OrderBy(b => b.FullName).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<BobBeneficiaryDto>> GetByIdAsync(Guid id)
    {
        var beneficiary = await beneficiaries.GetByIdAsync(id);
        return beneficiary is null
            ? ServiceResult<BobBeneficiaryDto>.NotFound("Beneficiarul nu există.")
            : ServiceResult<BobBeneficiaryDto>.Ok(ToDto(beneficiary));
    }

    public async Task<BobBeneficiaryDto> CreateAsync(CreateBobBeneficiaryRequest request)
    {
        var beneficiary = new BobBeneficiary
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            Mobility = request.Mobility,
            Address = request.Address,
            AssignedVolunteerName = request.AssignedVolunteerName,
            Notes = request.Notes
        };
        beneficiaries.Add(beneficiary);
        await unitOfWork.SaveChangesAsync();
        return ToDto(beneficiary);
    }

    /// <summary>Câmp-cu-câmp — folosit inclusiv pentru toggle rapid Contacted/Delivered/
    /// AssignedVolunteerName direct din listă, un câmp o dată.</summary>
    public async Task<ServiceResult<BobBeneficiaryDto>> UpdateAsync(Guid id, UpdateBobBeneficiaryRequest request)
    {
        var beneficiary = await beneficiaries.GetByIdAsync(id);
        if (beneficiary is null) return ServiceResult<BobBeneficiaryDto>.NotFound("Beneficiarul nu există.");

        if (request.FullName is not null) beneficiary.FullName = request.FullName;
        if (request.Phone is not null) beneficiary.Phone = request.Phone;
        if (request.Mobility is not null) beneficiary.Mobility = request.Mobility;
        if (request.Address is not null) beneficiary.Address = request.Address;
        if (request.AssignedVolunteerName is not null) beneficiary.AssignedVolunteerName = request.AssignedVolunteerName;
        if (request.Status is not null) beneficiary.Status = request.Status.Value;
        if (request.Contacted is not null) beneficiary.Contacted = request.Contacted.Value;
        if (request.Delivered is not null) beneficiary.Delivered = request.Delivered.Value;
        if (request.Notes is not null) beneficiary.Notes = request.Notes;
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<BobBeneficiaryDto>.Ok(ToDto(beneficiary));
    }

    private static BobBeneficiaryDto ToDto(BobBeneficiary b) => new(
        b.Id, b.FullName, b.Phone, b.Mobility, b.Address, b.AssignedVolunteerName, b.Status, b.Contacted, b.Delivered, b.Notes);
}
