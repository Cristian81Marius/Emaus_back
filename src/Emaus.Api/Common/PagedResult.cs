namespace Emaus.Api.Common;

/// <summary>Formă standard de paginare — vezi docs/API.md §1. Folosită deocamdată doar de
/// `GET /api/beneficiaries`.</summary>
public record PagedResult<T>(List<T> Items, int Page, int PageSize, int Total, bool HasMore);
