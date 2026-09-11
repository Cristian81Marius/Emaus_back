using Emaus.Domain;

namespace Emaus.Api.Dtos.Bookings;

public record BookingCommentDto(Guid Id, Guid AuthorUserId, string AuthorName, string Text, DateTime CreatedAt);

public record BookingDto(
    Guid Id, Guid BeneficiaryId, string BeneficiaryName,
    string? BeneficiaryPhone, // CALCULAT live din Beneficiary.Phone la fiecare răspuns, nu stocat
    Guid? UnitId, string? UnitName, string? PropertyAddress,
    DateOnly RequestedCheckIn, DateOnly RequestedCheckOut, DateOnly? ActualCheckIn, DateOnly? ActualCheckOut,
    BookingStatus Status, string CreatedByName, DateTime CreatedAt, string? DecidedByName,
    DateTime? DecidedAt, string? DecisionNote,
    Guid? CaseManagerUserId, string? CaseManagerName, // [NOU] — vezi docs/API.md §6
    List<BookingCommentDto> Comments);

/// <summary>Cererea inițială — fără unitate alocată încă; alocarea se face separat, la aprobare.</summary>
public record CreateBookingRequest(Guid BeneficiaryId, DateOnly RequestedCheckIn, DateOnly RequestedCheckOut);

/// <summary>Perioada CERUTĂ se editează doar înainte de alocare (PendingApproval/Approved) —
/// după alocare (Active), perioada reală (actualCheckIn/actualCheckOut) preia rolul.
/// `CaseManagerUserId` **[NOU]** se poate seta/schimba oricând, indiferent de status —
/// desemnarea managerului de caz nu ține de fluxul de aprobare/alocare.</summary>
public record UpdateBookingRequest(DateOnly? RequestedCheckIn, DateOnly? RequestedCheckOut, Guid? CaseManagerUserId);

public record AddCommentRequest(string Text);

public record DecideBookingRequest(bool Approved, string? Note);

/// <summary>Alocă o unitate liberă unei solicitări deja aprobate — pas separat de decizie,
/// pentru că nucleul poate aproba fără să știe încă exact ce unitate va fi liberă la timp.</summary>
public record AllocateUnitRequest(Guid UnitId);

public record CheckOutRequest(DateOnly? ActualCheckOutDate);

/// <summary>Anulează o solicitare care încă n-a ajuns la o unitate (În așteptare sau Aprobată) —
/// ex. beneficiarul renunță, sau nu mai are nevoie de cazare până la urmă. O cazare deja
/// Activă se încheie cu check-out, nu se anulează.</summary>
public record CancelBookingRequest(string? Reason);
