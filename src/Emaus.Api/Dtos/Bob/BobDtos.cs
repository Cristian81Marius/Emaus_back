using Emaus.Domain;

namespace Emaus.Api.Dtos.Bob;

// ---- 13.1 Beneficiari BOB -------------------------------------------------------------------

public record BobBeneficiaryDto(
    Guid Id, string FullName, string? Phone, BobMobility? Mobility, string? Address,
    string? AssignedVolunteerName, BobBeneficiaryStatus Status, bool Contacted, bool Delivered, string? Notes);

/// <summary>Body = orice subset de câmpuri (fără Id); implicit Status: Active, Contacted/Delivered: false.</summary>
public record CreateBobBeneficiaryRequest(
    string FullName, string? Phone, BobMobility? Mobility, string? Address,
    string? AssignedVolunteerName, string? Notes);

/// <summary>Body = orice subset, aplicat câmp-cu-câmp — folosit inclusiv pentru toggle rapid
/// Contacted/Delivered/AssignedVolunteerName direct din listă, un câmp o dată. Ca la restul
/// PATCH-urilor din API (ex. UpdatePropertyRequest): un câmp nul (omis SAU trimis explicit
/// null) rămâne neatins — nu există azi un caz de UI care ar avea nevoie să șteargă explicit
/// AssignedVolunteerName/Notes prin PATCH, ci doar să le înlocuiască cu altă valoare.</summary>
public record UpdateBobBeneficiaryRequest(
    string? FullName, string? Phone, BobMobility? Mobility, string? Address,
    string? AssignedVolunteerName, BobBeneficiaryStatus? Status, bool? Contacted, bool? Delivered, string? Notes);

// ---- 13.2 Catalog cutie ----------------------------------------------------------------------

public record BoxItemDto(Guid Id, string Name, decimal Price, bool Checked);

public record BoxCategoryDto(string Key, string Title, List<BoxItemDto> Items);

/// <summary>`Price` e `decimal` direct (JSON number pe fir) — mobilul normalizează orice
/// intrare cu virgulă în număr ÎNAINTE de a trimite (vezi `parseDecimal()` din
/// mobile/src/utils/number.ts), deci pe fir ajunge deja un JSON number valid, fără nicio
/// ambiguitate de separator zecimal (asta există doar pentru textul tastat de utilizator,
/// niciodată pentru formatul JSON în sine).</summary>
public record UpdateBoxItemRequest(bool? Checked, decimal? Price, string? Name);

public record CreateBoxItemRequest(string CategoryKey, string Name, decimal Price);

// ---- 13.3 Istoric cumpărături ------------------------------------------------------------------

public record BobPurchaseItemDto(string Name, decimal Price);

public record BobDeliveryRecordDto(Guid Id, string Date, List<BobPurchaseItemDto> Items, decimal Total, string? Note);

public record CreatePurchaseRequest(string? Note);

// ---- 13.4 Dashboard BOB -----------------------------------------------------------------------

public record BobStatsDto(
    int BeneficiariesCount, decimal MaxBudgetPerBox, int DaysUntilNextDelivery,
    string? LastDeliveryDate, decimal CurrentBoxTotal, int VolunteersInvolved);
