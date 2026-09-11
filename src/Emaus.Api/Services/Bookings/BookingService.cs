using Emaus.Api.Common;
using Emaus.Api.Dtos.Bookings;
using Emaus.Api.Services.Contracts;
using Emaus.Api.Services.Notifications;
using Emaus.Domain;
using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace Emaus.Api.Services.Bookings;

/// <summary>Fluxul central al aplicației — vezi secțiunea "Flux: solicitare nouă" din plan:
/// cerere → notificare nucleu → comentarii → decizie → alocare unitate → check-out automat
/// spre curățenie. Fiecare metodă corespunde unui pas, pentru că fiecare poate fi făcut de
/// altcineva, în alt moment. Controllerul (BookingsController) doar traduce HTTP ↔ apeluri aici.</summary>
public class BookingService(
    IRepository<Booking> bookings,
    IRepository<Beneficiary> beneficiaries,
    IRepository<Unit> units,
    IRepository<CleaningTask> cleaningTasks,
    IRepository<CleaningAssignment> cleaningAssignments,
    IRepository<ApplicationUser> users,
    IUnitOfWork unitOfWork,
    NotificationService notifications)
{
    /// <summary>`unitId`/`propertyId` sunt folosite de ecranul unei locații ca să arate
    /// "cine stă acum aici + istoricul" — vezi PropertyDetailScreen pe mobil. O solicitare
    /// abia creată/aprobată nu are încă `UnitId` (se leagă de o unitate doar la alocare),
    /// deci filtrarea pe locație arată mereu doar cazări Active/Completed pentru acea unitate,
    /// nu și cererile din coadă care încă așteaptă o unitate — acelea rămân vizibile doar
    /// nefiltrat (`GET /api/bookings?status=Approved`), pentru că nu au încă o locație.</summary>
    public async Task<List<BookingDto>> GetAllAsync(BookingStatus? status, Guid? unitId, Guid? propertyId, Guid? beneficiaryId = null)
    {
        var query = BaseQuery();
        if (status is not null) query = query.Where(b => b.Status == status);
        if (unitId is not null) query = query.Where(b => b.UnitId == unitId);
        if (propertyId is not null) query = query.Where(b => b.Unit != null && b.Unit.PropertyId == propertyId);
        // beneficiaryId — folosit de fișa unui beneficiar (app/beneficiaries/[id].tsx) ca să-i
        // arate întreg istoricul de cazări, indiferent de locație (spre deosebire de unitId/
        // propertyId de mai sus, care sunt legate de o locație anume).
        if (beneficiaryId is not null) query = query.Where(b => b.BeneficiaryId == beneficiaryId);

        var list = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<BookingDto>> GetByIdAsync(Guid id)
    {
        var booking = await BaseQuery().SingleOrDefaultAsync(b => b.Id == id);
        return booking is null
            ? ServiceResult<BookingDto>.NotFound("Solicitarea nu există.")
            : ServiceResult<BookingDto>.Ok(ToDto(booking));
    }

    public async Task<ServiceResult<BookingDto>> CreateAsync(CreateBookingRequest request, Guid createdByUserId)
    {
        var beneficiary = await beneficiaries.GetByIdAsync(request.BeneficiaryId);
        if (beneficiary is null) return ServiceResult<BookingDto>.NotFound("Beneficiarul nu există.");
        if (beneficiary.Status == BeneficiaryStatus.Blocked)
        {
            return ServiceResult<BookingDto>.Conflict($"Beneficiarul e blocat: {beneficiary.BlockedReason}");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BeneficiaryId = request.BeneficiaryId,
            RequestedCheckIn = request.RequestedCheckIn,
            RequestedCheckOut = request.RequestedCheckOut,
            Status = BookingStatus.PendingApproval,
            CreatedByUserId = createdByUserId
        };
        bookings.Add(booking);
        await unitOfWork.SaveChangesAsync();

        await notifications.NotifyRoleAsync(UserRole.Nucleus, NotificationType.NewBookingRequest,
            $"Solicitare nouă pentru {beneficiary.FullName} ({request.RequestedCheckIn:dd.MM} – {request.RequestedCheckOut:dd.MM})",
            nameof(Booking), booking.Id);

        var created = await BaseQuery().SingleAsync(b => b.Id == booking.Id);
        return ServiceResult<BookingDto>.Ok(ToDto(created));
    }

    /// <summary>Perioada CERUTĂ se editează doar înainte de alocare (PendingApproval/Approved) —
    /// după alocare (Active), perioada reală preia rolul, cererea inițială nu se mai atinge.
    /// `CaseManagerUserId` e independent de asta — se poate seta/schimba oricând, indiferent
    /// de status, deci verificarea de conflict se aplică DOAR dacă se schimbă datele cerute.</summary>
    public async Task<ServiceResult<BookingDto>> UpdateAsync(Guid id, UpdateBookingRequest request)
    {
        var booking = await bookings.GetByIdAsync(id);
        if (booking is null) return ServiceResult<BookingDto>.NotFound("Solicitarea nu există.");

        var changesRequestedDates = request.RequestedCheckIn is not null || request.RequestedCheckOut is not null;
        if (changesRequestedDates && booking.Status is not (BookingStatus.PendingApproval or BookingStatus.Approved))
        {
            return ServiceResult<BookingDto>.Conflict("Perioada cerută se poate edita doar înainte de alocare.");
        }

        if (request.RequestedCheckIn is not null) booking.RequestedCheckIn = request.RequestedCheckIn.Value;
        if (request.RequestedCheckOut is not null) booking.RequestedCheckOut = request.RequestedCheckOut.Value;

        if (request.CaseManagerUserId is not null)
        {
            var caseManager = await users.GetByIdAsync(request.CaseManagerUserId.Value);
            if (caseManager is null) return ServiceResult<BookingDto>.NotFound("Utilizatorul desemnat ca manager de caz nu există.");
            booking.CaseManagerUserId = request.CaseManagerUserId;
        }

        await unitOfWork.SaveChangesAsync();

        var updated = await BaseQuery().SingleAsync(b => b.Id == id);
        return ServiceResult<BookingDto>.Ok(ToDto(updated));
    }

    public async Task<ServiceResult<BookingCommentDto>> AddCommentAsync(Guid id, AddCommentRequest request, Guid authorUserId)
    {
        var booking = await bookings.GetByIdAsync(id);
        if (booking is null) return ServiceResult<BookingCommentDto>.NotFound("Solicitarea nu există.");

        var comment = new BookingComment
        {
            Id = Guid.NewGuid(),
            BookingId = id,
            AuthorUserId = authorUserId,
            Text = request.Text
        };
        booking.Comments.Add(comment);
        await unitOfWork.SaveChangesAsync();

        await notifications.NotifyRoleAsync(UserRole.Nucleus, NotificationType.NewCommentOnBooking,
            "Comentariu nou pe o solicitare", nameof(Booking), id);

        var author = await users.GetByIdAsync(authorUserId);
        // Nu ar trebui să fie null (tokenul e deja verificat la autentificare — vezi
        // Program.cs, OnTokenValidated), dar dacă totuși ajunge aici cu un cont șters
        // între emiterea tokenului și acest apel, întoarcem un mesaj clar în loc de un
        // NullReferenceException opac (500 "eroare neașteptată" fără nicio pistă).
        if (author is null) return ServiceResult<BookingCommentDto>.NotFound("Contul tău nu mai există — reconectează-te.");

        return ServiceResult<BookingCommentDto>.Ok(
            new BookingCommentDto(comment.Id, comment.AuthorUserId, author.FullName, comment.Text, comment.CreatedAt));
    }

    public async Task<ServiceResult<BookingDto>> DecideAsync(Guid id, DecideBookingRequest request, Guid decidedByUserId)
    {
        var booking = await bookings.Query().Include(b => b.Beneficiary).SingleOrDefaultAsync(b => b.Id == id);
        if (booking is null) return ServiceResult<BookingDto>.NotFound("Solicitarea nu există.");
        if (booking.Status != BookingStatus.PendingApproval)
        {
            return ServiceResult<BookingDto>.Conflict("Solicitarea a fost deja decisă.");
        }

        booking.Status = request.Approved ? BookingStatus.Approved : BookingStatus.Rejected;
        booking.DecidedByUserId = decidedByUserId;
        booking.DecidedAt = DateTime.UtcNow;
        booking.DecisionNote = request.Note;
        await unitOfWork.SaveChangesAsync();

        var verdict = request.Approved ? "aprobată" : "respinsă";
        await notifications.NotifyUserAsync(booking.CreatedByUserId, NotificationType.BookingDecided,
            $"Solicitarea pentru {booking.Beneficiary.FullName} a fost {verdict}.", nameof(Booking), id);

        var updated = await BaseQuery().SingleAsync(b => b.Id == id);
        return ServiceResult<BookingDto>.Ok(ToDto(updated));
    }

    /// <summary>Alocă o unitate liberă unei solicitări aprobate. Pas separat de decizie
    /// (vezi comentariul de pe AllocateUnitRequest) — unitatea trebuie să fie Available acum.</summary>
    public async Task<ServiceResult<BookingDto>> AllocateAsync(Guid id, AllocateUnitRequest request)
    {
        var booking = await bookings.GetByIdAsync(id);
        if (booking is null) return ServiceResult<BookingDto>.NotFound("Solicitarea nu există.");
        if (booking.Status != BookingStatus.Approved)
        {
            return ServiceResult<BookingDto>.Conflict("Solicitarea trebuie să fie aprobată înainte de alocare.");
        }

        var unit = await units.GetByIdAsync(request.UnitId);
        if (unit is null) return ServiceResult<BookingDto>.NotFound("Unitatea nu există.");
        if (unit.Status != UnitStatus.Available)
        {
            return ServiceResult<BookingDto>.Conflict($"Unitatea nu e disponibilă (status curent: {unit.Status}).");
        }

        booking.UnitId = unit.Id;
        booking.ActualCheckIn = DateOnly.FromDateTime(DateTime.UtcNow);
        booking.Status = BookingStatus.Active;
        unit.Status = UnitStatus.Occupied;
        await unitOfWork.SaveChangesAsync();

        var updated = await BaseQuery().SingleAsync(b => b.Id == id);
        return ServiceResult<BookingDto>.Ok(ToDto(updated));
    }

    /// <summary>La check-out, unitatea trece automat în NeedsCleaning și se deschide o tură
    /// de curățenie — vezi secțiunea "Curățenie & chei" din plan.</summary>
    public async Task<ServiceResult<BookingDto>> CheckOutAsync(Guid id, CheckOutRequest request)
    {
        var booking = await bookings.Query().Include(b => b.Unit).SingleOrDefaultAsync(b => b.Id == id);
        if (booking is null) return ServiceResult<BookingDto>.NotFound("Solicitarea nu există.");
        if (booking.Status != BookingStatus.Active || booking.Unit is null)
        {
            return ServiceResult<BookingDto>.Conflict("Doar o cazare activă poate fi încheiată.");
        }

        var checkOutDate = request.ActualCheckOutDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        booking.ActualCheckOut = checkOutDate;
        booking.Status = BookingStatus.Completed;
        booking.Unit.Status = UnitStatus.NeedsCleaning;

        cleaningTasks.Add(new CleaningTask
        {
            Id = Guid.NewGuid(),
            UnitId = booking.Unit.Id,
            ScheduledDate = checkOutDate,
            Status = CleaningTaskStatus.Pending
        });
        await unitOfWork.SaveChangesAsync();

        var cleaningVolunteerIds = await cleaningAssignments.Query()
            .Where(a => a.PropertyId == booking.Unit.PropertyId)
            .Select(a => a.VolunteerUserId)
            .Distinct()
            .ToListAsync();

        if (cleaningVolunteerIds.Count > 0)
        {
            await notifications.NotifyUsersAsync(cleaningVolunteerIds, NotificationType.UnitNeedsCleaning,
                $"{booking.Unit.Name} necesită curățenie după eliberare.", nameof(Unit), booking.Unit.Id);
        }

        var updated = await BaseQuery().SingleAsync(b => b.Id == id);
        return ServiceResult<BookingDto>.Ok(ToDto(updated));
    }

    /// <summary>Anulează o solicitare care încă n-a ajuns la o unitate (În așteptare/Aprobată).
    /// O cazare deja Activă nu se anulează — se încheie cu check-out (vezi CheckOutAsync).</summary>
    public async Task<ServiceResult<BookingDto>> CancelAsync(Guid id, CancelBookingRequest request, Guid cancelledByUserId)
    {
        var booking = await bookings.Query().Include(b => b.Beneficiary).SingleOrDefaultAsync(b => b.Id == id);
        if (booking is null) return ServiceResult<BookingDto>.NotFound("Solicitarea nu există.");
        if (booking.Status is not (BookingStatus.PendingApproval or BookingStatus.Approved))
        {
            return ServiceResult<BookingDto>.Conflict("Doar o solicitare încă nealocată unei unități poate fi anulată.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.DecidedByUserId = cancelledByUserId;
        booking.DecidedAt = DateTime.UtcNow;
        booking.DecisionNote = request.Reason;
        await unitOfWork.SaveChangesAsync();

        await notifications.NotifyUserAsync(booking.CreatedByUserId, NotificationType.BookingDecided,
            $"Solicitarea pentru {booking.Beneficiary.FullName} a fost anulată.", nameof(Booking), id);

        var updated = await BaseQuery().SingleAsync(b => b.Id == id);
        return ServiceResult<BookingDto>.Ok(ToDto(updated));
    }

    /// <summary>Generează PDF-ul "Contract de acordare a serviciilor sociale" (vezi
    /// BookingContractDocument) — câmpurile lipsă (beneficiarul n-are încă C.I./adresă/
    /// persoană de sprijin completate, sau nu s-a desemnat încă un manager de caz) apar ca
    /// linie de completat manual, nu blochează generarea.</summary>
    public async Task<ServiceResult<byte[]>> GenerateContractPdfAsync(Guid id)
    {
        var booking = await bookings.Query()
            .Include(b => b.Beneficiary).ThenInclude(ben => ben.Locality)
            .Include(b => b.CaseManagerUser)
            .SingleOrDefaultAsync(b => b.Id == id);
        if (booking is null) return ServiceResult<byte[]>.NotFound("Solicitarea nu există.");

        var data = new BookingContractData(
            BeneficiaryFullName: booking.Beneficiary.FullName,
            BeneficiaryLocalityName: booking.Beneficiary.Locality?.Name ?? booking.Beneficiary.LocalityFreeText,
            BeneficiaryCounty: booking.Beneficiary.Locality?.County,
            BeneficiaryAddress: booking.Beneficiary.Address,
            BeneficiaryIdCardSeries: booking.Beneficiary.IdCardSeries,
            BeneficiaryIdCardNumber: booking.Beneficiary.IdCardNumber,
            BeneficiaryPhone: booking.Beneficiary.Phone,
            SupportPersonName: booking.Beneficiary.SupportPersonName,
            SupportPersonPhone: booking.Beneficiary.SupportPersonPhone,
            CaseManagerName: booking.CaseManagerUser?.FullName,
            CaseManagerPhone: booking.CaseManagerUser?.Phone,
            RequestedCheckIn: booking.RequestedCheckIn,
            RequestedCheckOut: booking.RequestedCheckOut);

        var pdfBytes = new BookingContractDocument(data).GeneratePdf();
        return ServiceResult<byte[]>.Ok(pdfBytes);
    }

    private IQueryable<Booking> BaseQuery() => bookings.Query()
        .Include(b => b.Beneficiary)
        .Include(b => b.Unit).ThenInclude(u => u!.Property)
        .Include(b => b.CreatedByUser)
        .Include(b => b.DecidedByUser)
        .Include(b => b.CaseManagerUser)
        .Include(b => b.Comments).ThenInclude(c => c.AuthorUser);

    private static BookingDto ToDto(Booking b) => new(
        b.Id, b.BeneficiaryId, b.Beneficiary.FullName, b.Beneficiary.Phone,
        b.UnitId, b.Unit?.Name, b.Unit?.Property.Address,
        b.RequestedCheckIn, b.RequestedCheckOut, b.ActualCheckIn, b.ActualCheckOut,
        b.Status, b.CreatedByUser.FullName, b.CreatedAt, b.DecidedByUser?.FullName,
        b.DecidedAt, b.DecisionNote,
        b.CaseManagerUserId, b.CaseManagerUser?.FullName,
        b.Comments.OrderBy(c => c.CreatedAt)
            .Select(c => new BookingCommentDto(c.Id, c.AuthorUserId, c.AuthorUser.FullName, c.Text, c.CreatedAt))
            .ToList());
}
