namespace Emaus.Domain;

/// <summary>Rolul unui utilizator în aplicație. MVP ține doar la două roluri, exact cum
/// funcționează echipa azi — orice decizie (aprobare, blocare, alocare) cere Nucleu.</summary>
public enum UserRole
{
    Volunteer = 0,
    Nucleus = 1
}

/// <summary>Statusul unui cont — separat de `IsActive` (dezactivare ulterioară a unui cont deja
/// aprobat). Un cont nou, auto-înregistrat, pornește `PendingApproval` și nu se poate loga până
/// nu decide cineva din Nucleus; conturile create direct de Nucleus (`POST /api/users`) sau la
/// seed pornesc direct `Active`.</summary>
public enum UserStatus
{
    PendingApproval = 0,
    Active = 1,
    Rejected = 2
}

/// <summary>Statusul unei unități cazabile (cameră sau apartament întreg).
/// Corespunde 1:1 valorilor folosite azi în Excel ("Ocupat", "Liber", "În așteptare"),
/// plus NeedsCleaning ca stare de tranziție explicită între check-out și disponibilitate reală.</summary>
public enum UnitStatus
{
    Available = 0,       // Liber & igienizat — poate fi alocată imediat
    Occupied = 1,         // Ocupat
    NeedsCleaning = 2,     // golit, dar nu încă igienizat
    CleaningInProgress = 3,
    Unavailable = 4        // blocat / reparație / alt motiv care o scoate din circuit
}

/// <summary>Statusul unei solicitări/cazări, de la cerere până la închidere.</summary>
public enum BookingStatus
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    Active = 3,      // beneficiarul e prezent acum
    Completed = 4,    // check-out efectuat
    Cancelled = 5      // anulată înainte de decizie sau de sosire
}

/// <summary>Statusul unui beneficiar. Reflectă cazul real întâlnit în evidență
/// ("Blocat din 15 septembrie!!").</summary>
public enum BeneficiaryStatus
{
    Active = 0,
    Blocked = 1
}

/// <summary>Tipul unei sesizări de mentenanță — consumabile curente vs. reparație vs. urgență.</summary>
public enum MaintenanceTicketType
{
    Supplies = 0,   // ex. hârtie igienică, becuri
    Repair = 1,      // ex. robinet stricat
    Urgent = 2        // ex. infiltrație, ușă care nu se mai încuie
}

public enum MaintenanceTicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum MaintenanceTicketStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    Resolved = 3
}

/// <summary>Statusul unei ture de curățenie (apariție concretă, nu abonamentul rotativ).</summary>
public enum CleaningTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Done = 2
}

/// <summary>Tipul unei oportunități de implicare pentru voluntari.</summary>
public enum OpportunityType
{
    Cleaning = 0,
    Event = 1,
    Visit = 2,
    Promotion = 3
}

/// <summary>Tipul unei notificări — determină cui îi ajunge și ce ecran deschide la tap.</summary>
public enum NotificationType
{
    NewBookingRequest = 0,
    BookingDecided = 1,
    NewCommentOnBooking = 2,
    UnitNeedsCleaning = 3,
    NewMaintenanceTicket = 4,
    MaintenanceTicketAssigned = 5,
    NewOpportunityPublished = 6,
    NewUserRequest = 7
}

/// <summary>Statusul unui beneficiar Box of Blessing — vine dintr-un tabel original cu secțiuni
/// separate ("Foști beneficiari"/"Posibili beneficiari"); UI-ul mobil arată implicit doar Active.</summary>
public enum BobBeneficiaryStatus
{
    Active = 0,
    Former = 1,
    Possible = 2
}

/// <summary>Mobilitatea unui beneficiar Box of Blessing — determină dacă poate veni singur
/// după cutie sau are nevoie de livrare la domiciliu.</summary>
public enum BobMobility
{
    Deplasabil = 0,
    Nedeplasabil = 1
}
