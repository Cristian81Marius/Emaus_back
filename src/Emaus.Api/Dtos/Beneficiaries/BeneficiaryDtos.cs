using Emaus.Domain;

namespace Emaus.Api.Dtos.Beneficiaries;

public record BeneficiaryDto(
    Guid Id, string FullName, string? Phone, int? LocalityId, string? LocalityName,
    string? LocalityFreeText, string? Address, string? IdCardSeries, string? IdCardNumber,
    string? SupportPersonName, string? SupportPersonPhone,
    int? Age, string? MaterialSituation, string? ReferralSource,
    BeneficiaryStatus Status, string? BlockedReason, decimal AccountBalance, string? Notes);

/// <summary>`Address`/`IdCardSeries`/`IdCardNumber`/`SupportPersonName`/`SupportPersonPhone`
/// **[NOU]** — cerute de contractul de acordare a serviciilor sociale, nu doar de fișa
/// internă (vezi docs/API.md §5).</summary>
public record CreateBeneficiaryRequest(
    string FullName, string? Phone, int? LocalityId, string? LocalityFreeText, string? Address,
    string? IdCardSeries, string? IdCardNumber, string? SupportPersonName, string? SupportPersonPhone,
    int? Age, string? MaterialSituation, string? ReferralSource, string? Notes);

/// <summary>Aceleași câmpuri ca la creare — NU status/blockedReason/accountBalance, alea sunt
/// treaba rutelor de block/unblock.</summary>
public record UpdateBeneficiaryRequest(
    string? FullName, string? Phone, int? LocalityId, string? LocalityFreeText, string? Address,
    string? IdCardSeries, string? IdCardNumber, string? SupportPersonName, string? SupportPersonPhone,
    int? Age, string? MaterialSituation, string? ReferralSource, string? Notes);

public record BlockBeneficiaryRequest(string Reason);
