using Emaus.Domain;
using Emaus.Domain.Bob;
using Emaus.Domain.Entities;
using Emaus.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace Emaus.Infrastructure.Data;

/// <summary>Populează baza de date, la prima pornire, cu datele reale din
/// <c>docs/SEED_DATA.md</c> — utilizatori, locații/unități, beneficiari, cazări (calendarul
/// real la momentul preluării evidenței), rotația de curățenie, plus eșantioanele sintetice
/// de mentenanță/activități/notificări și tot domeniul Box of Blessing (beneficiari, catalog
/// de cutie). Rulează o singură dată — verifică <see cref="AppDbContext.Users"/> înainte de
/// a semăna. Dacă schema se schimbă (entitate nouă), <c>emaus.db</c> trebuie ștearsă manual
/// ca <c>EnsureCreated</c> să o recreeze cu noul schema (vezi CLAUDE.md).</summary>
public static class DbSeeder
{
    /// <summary>Parola tuturor conturilor de test seed-uite — de schimbat imediat într-un
    /// mediu real, există doar ca să nu se pornească de la o bază goală.</summary>
    public const string SeedPassword = "Emaus2026!";

    public static void Seed(AppDbContext db, ILogger logger)
    {
        if (db.Users.Any())
        {
            logger.LogInformation("Baza de date are deja date — seed sărit.");
            return;
        }

        logger.LogInformation("Baza de date e goală — populez cu date inițiale din docs/SEED_DATA.md...");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var users = SeedUsers(db);
        SeedLocalities(db);
        var (properties, units) = SeedPropertiesAndUnits(db);
        var beneficiaries = SeedBeneficiaries(db);
        SeedBookings(db, beneficiaries, units, users);
        SeedCleaning(db, properties, units, users, today);
        SeedMaintenance(db, properties, users, today);
        SeedOpportunities(db, properties, users, today);
        SeedNotifications(db, users, today);
        SeedOrganizationSettings(db);

        SeedBobBeneficiaries(db);
        SeedBobBox(db);
        db.BobSettings.Add(new BobSettings { Id = Guid.NewGuid(), MaxBudgetPerBox = 99.28m });

        db.SaveChanges();

        logger.LogInformation(
            "Seed complet: {Properties} proprietăți, {Users} utilizatori, {Beneficiaries} beneficiari Emaus. " +
            "Cont Nucleus: {Phone} / parola din DbSeeder.SeedPassword.",
            properties.Count, users.Count, beneficiaries.Count, "0700000000");
    }

    // ---- Utilizatori ------------------------------------------------------------------------

    private static Dictionary<string, ApplicationUser> SeedUsers(AppDbContext db)
    {
        (string Name, string Phone, string? Email, UserRole Role)[] spec =
        [
            ("Alexandra", "0700000000", "alexandra@emaus.ro", UserRole.Nucleus),
            ("David", "0711111111", null, UserRole.Volunteer),
            ("Alina", "0722010203", null, UserRole.Volunteer),
            ("Ligia", "0722020304", null, UserRole.Volunteer),
            ("Ioana", "0722030405", null, UserRole.Volunteer),
            ("Ștefan", "0722040506", null, UserRole.Volunteer),
            ("Dominic", "0722050607", null, UserRole.Volunteer),
            ("Ema", "0722060708", null, UserRole.Volunteer),
            ("Sami", "0722070809", null, UserRole.Volunteer),
            ("Cristi", "0722080910", null, UserRole.Volunteer),
            ("Rebeca", "0722091011", null, UserRole.Volunteer),
            ("Elena", "0722101112", null, UserRole.Volunteer),
            ("Miriam", "0722111213", null, UserRole.Volunteer),
            ("Florentina", "0722121314", null, UserRole.Volunteer),
            ("Diandra", "0722131415", null, UserRole.Volunteer),
        ];

        var users = new Dictionary<string, ApplicationUser>();
        foreach (var (name, phone, email, role) in spec)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = name,
                Phone = phone,
                Email = email,
                PasswordHash = PasswordHasher.Hash(SeedPassword),
                Role = role
            };
            db.Users.Add(user);
            users[name] = user;
        }
        return users;
    }

    // ---- Localități -------------------------------------------------------------------------

    private static void SeedLocalities(AppDbContext db)
    {
        db.Localities.AddRange(
            new Locality { Name = "București", County = "București" },
            new Locality { Name = "Iași", County = "Iași" },
            new Locality { Name = "Constanța", County = "Constanța" },
            new Locality { Name = "Galați", County = "Galați" },
            new Locality { Name = "Brăila", County = "Brăila" },
            new Locality { Name = "Bacău", County = "Bacău" },
            new Locality { Name = "Neamț", County = "Neamț" },
            new Locality { Name = "Botoșani", County = "Botoșani" },
            new Locality { Name = "Vrancea", County = "Vrancea" },
            new Locality { Name = "Mehedinți", County = "Mehedinți" },
            new Locality { Name = "Altă localitate din România", County = null },
            new Locality { Name = "Altă țară", County = null, Country = null });
    }

    // ---- Locații & unități ------------------------------------------------------------------

    private static (Dictionary<string, Property> Properties, Dictionary<string, Unit> Units) SeedPropertiesAndUnits(AppDbContext db)
    {
        var properties = new Dictionary<string, Property>();
        var units = new Dictionary<string, Unit>();

        Property AddProperty(string key, string address, string shortLabel, string? interfon,
            string[] keyHolders, string? keyNotes, int? lifetimeStayDays, int? lifetimeBookingsCompleted,
            bool isTemporary = false, string? notes = null)
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                Address = address,
                ShortLabel = shortLabel,
                IsTemporary = isTemporary,
                Notes = notes,
                Interfon = interfon,
                KeyHolders = keyHolders.ToList(),
                KeyNotes = keyNotes,
                LifetimeStayDays = lifetimeStayDays,
                LifetimeBookingsCompleted = lifetimeBookingsCompleted
            };
            db.Properties.Add(property);
            properties[key] = property;
            return property;
        }

        void AddUnit(string key, Property property, string name, int capacity)
        {
            var unit = new Unit { Id = Guid.NewGuid(), PropertyId = property.Id, Property = property, Name = name, Capacity = capacity };
            db.Units.Add(unit);
            units[key] = unit;
        }

        var laborator124 = AddProperty("prop-laborator-124",
            "Str. Laborator, nr. 124, bl. 40, sc. A, et. 11, ap. 124", "Str. Laborator nr. 124, ap. 124",
            null, ["Alexandra", "David"], "1 cheie - la beneficiari", 997, 58);
        AddUnit("unit-laborator-124", laborator124, "Ap. 124", 3);

        var laborator94 = AddProperty("prop-laborator-94",
            "Str. Laborator, nr. 124, bl. 40, sc. A, et. 8, ap. 94", "Str. Laborator nr. 124, ap. 94",
            null, ["Alexandra", "Ștefan", "Ligia", "Diandra"], "1 cheie - la beneficiari", 789, 52);
        AddUnit("unit-laborator-94", laborator94, "Ap. 94", 3);

        var dristorului893 = AddProperty("prop-dristorului-893",
            "Str. Dristorului, nr. 97-119, bl. 63, sc. 3, et. 9, ap. 893", "Str. Dristorului nr. 97-119, ap. 893",
            "9316 c (SC 3 stânga)", ["Alexandra", "Ștefan", "Ligia"], "1 cheie la beneficiari", 742, 32);
        AddUnit("unit-dristorului-893", dristorului893, "Ap. 893", 3);

        var dristoruluiParter = AddProperty("prop-dristorului-parter",
            "Str. Dristorului, nr. 97-119 — Parter", "Str. Dristorului nr. 97-119, parter",
            null, [], null, null, null);
        AddUnit("unit-dristorului-parter", dristoruluiParter, "Parter", 2);

        var vladJudetul = AddProperty("prop-vlad-judetul",
            "Str. Vlad Județul, nr. 4, bl. V13, sc. 1, et. 3, ap. 37", "Str. Vlad Județul nr. 4, ap. 37",
            null, ["Alexandra", "Ștefan"], "1 cheie la beneficiari", 568, 29);
        AddUnit("unit-vlad-judetul", vladJudetul, "Ap. 37", 2);

        var traianPopovici = AddProperty("prop-traian-popovici",
            "Str. Traian Popovici, nr. 132, bl. B3D, sc. A, et. 5, ap. 28", "Str. Traian Popovici nr. 132, ap. 28",
            "528", ["Alexandra", "Cristi"], "2 chei la beneficiari", 523, 51);
        AddUnit("unit-traian-sufragerie", traianPopovici, "Sufragerie", 3);
        AddUnit("unit-traian-dormitor", traianPopovici, "Dormitor", 2);

        var stanescuGheorghe = AddProperty("prop-stanescu-gheorghe",
            "Str. Stănescu Gheorghe, nr. 1, bl. 213, sc. A, et. 1, ap. 6", "Str. Stănescu Gheorghe nr. 1, ap. 6",
            null, ["Alexandra", "Miriam"], "2 chei la beneficiari", 191, 25);
        AddUnit("unit-stanescu-sufragerie", stanescuGheorghe, "Sufragerie", 3);
        AddUnit("unit-stanescu-dormitor", stanescuGheorghe, "Dormitor", 2);

        var elevStefanescu = AddProperty("prop-elev-stefanescu",
            "Str. Elev Ștefan Ștefănescu, nr. 9, bl. 449, sc. A, et. 2, ap. 32", "Str. Elev Ștefan Ștefănescu nr. 9, ap. 32",
            null, [], null, 801, 37);
        AddUnit("unit-elev-stefanescu", elevStefanescu, "Ap. 32", 3);

        var airbnb = AddProperty("prop-airbnb-temporar", "Locații AirBnB / temporare", "Locații temporare",
            null, [], null, 94, 25, isTemporary: true,
            notes: "Cazări scurte, ad-hoc, în afara celor 7 locații fixe — rezervate punctual când e nevoie.");
        AddUnit("unit-airbnb", airbnb, "Locație temporară", 2);

        return (properties, units);
    }

    // ---- Beneficiari --------------------------------------------------------------------------

    private static Dictionary<string, Beneficiary> SeedBeneficiaries(AppDbContext db)
    {
        var beneficiaries = new Dictionary<string, Beneficiary>();

        Beneficiary Add(string fullName, string? phone = null, string? localityFreeText = null,
            BeneficiaryStatus status = BeneficiaryStatus.Active, string? blockedReason = null, string? notes = null)
        {
            var beneficiary = new Beneficiary
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Phone = phone,
                LocalityFreeText = localityFreeText,
                Status = status,
                BlockedReason = blockedReason,
                BlockedAt = blockedReason is not null ? DateTime.UtcNow : null,
                Notes = notes
            };
            db.Beneficiaries.Add(beneficiary);
            beneficiaries[fullName] = beneficiary;
            return beneficiary;
        }

        Add("Cotulbea Marian");
        Add("Șerban Emilia");
        Add("Nuțu Dumitru");
        Add("Cianca Mihalache");
        Add("Micu Ramona");
        Add("Tirsoreanu Matilda");
        Add("Daniela Adina");
        Add("Mama și copil");
        Add("Fam. Bulandra");
        Add("Beneficiar hospice", notes: "Pacient în îngrijire paliativă.");
        Add("Olariu", status: BeneficiaryStatus.Blocked, blockedReason: "Motiv nespecificat în evidența preluată.");
        Add("Jingă Florina");
        Add("Bărculescu Claudia");
        Add("Fam. Duroi", localityFreeText: "Mehedinți");
        Add("Andrei Mihai");
        Add("Silochi Viorica");
        Add("Ovcearenco Victor");
        Add("Mazilu Florin");
        Add("Ivan Erik", phone: "0766884441", localityFreeText: "Constanța");
        Add("Chesa Dumitru", phone: "0726852804", localityFreeText: "Constanța");
        Add("Fam. Enache");

        return beneficiaries;
    }

    // ---- Solicitări & cazări --------------------------------------------------------------------

    private sealed record BookingSpec(
        string BeneficiaryName, string? UnitKey, DateOnly RequestedIn, DateOnly RequestedOut,
        DateOnly? ActualIn, DateOnly? ActualOut, BookingStatus Status, string RequestedByName,
        string? DecidedByName = null, string? DecisionNote = null, (string Author, string Text, DateOnly Date)? Comment = null);

    private static void SeedBookings(
        AppDbContext db, Dictionary<string, Beneficiary> beneficiaries, Dictionary<string, Unit> units,
        Dictionary<string, ApplicationUser> users)
    {
        BookingSpec[] specs =
        [
            // Active — ocupanții curenți (6)
            new("Cotulbea Marian", "unit-dristorului-893", new(2026, 8, 31), new(2026, 10, 9), new(2026, 8, 31), null,
                BookingStatus.Active, "Ștefan", "Alexandra",
                Comment: ("Ligia", "Cotulbea Marian s-a instalat, totul e în regulă.", new(2026, 8, 31))),
            new("Șerban Emilia", "unit-vlad-judetul", new(2026, 7, 2), new(2026, 9, 30), new(2026, 7, 2), null,
                BookingStatus.Active, "Alexandra", "Alexandra"),
            new("Nuțu Dumitru", "unit-traian-sufragerie", new(2026, 8, 10), new(2026, 10, 10), new(2026, 8, 10), null,
                BookingStatus.Active, "Cristi", "Alexandra"),
            new("Cianca Mihalache", "unit-traian-dormitor", new(2026, 8, 17), new(2026, 10, 9), new(2026, 8, 17), null,
                BookingStatus.Active, "Cristi", "Alexandra"),
            new("Micu Ramona", "unit-stanescu-sufragerie", new(2026, 8, 31), new(2026, 9, 18), new(2026, 8, 31), null,
                BookingStatus.Active, "Miriam", "Alexandra"),
            new("Tirsoreanu Matilda", "unit-stanescu-dormitor", new(2026, 9, 9), new(2026, 9, 11), new(2026, 9, 9), null,
                BookingStatus.Active, "Miriam", "Alexandra"),

            // În așteptare (2)
            new("Daniela Adina", null, new(2026, 9, 15), new(2026, 10, 13), null, null, BookingStatus.PendingApproval, "Ligia"),
            new("Mama și copil", null, new(2026, 9, 14), new(2026, 9, 25), null, null, BookingStatus.PendingApproval, "Ligia"),

            // Aprobate, nealocate încă (2)
            new("Fam. Bulandra", null, new(2026, 9, 16), new(2026, 9, 24), null, null, BookingStatus.Approved, "Alexandra", "Alexandra"),
            new("Beneficiar hospice", null, new(2026, 9, 25), new(2026, 10, 9), null, null, BookingStatus.Approved, "Alexandra", "Alexandra"),

            // Respinsă (1)
            new("Olariu", null, new(2026, 9, 13), new(2026, 9, 15), null, null, BookingStatus.Rejected, "Ligia", "Alexandra",
                DecisionNote: "Beneficiar blocat."),

            // Anulată (1)
            new("Fam. Enache", null, new(2026, 9, 20), new(2026, 9, 28), null, null, BookingStatus.Cancelled, "David", "Alexandra"),

            // Încheiate — eșantion din istoric (10 + 1 transcris ca "Fam. Corduneanu" -> leagă spre Fam. Enache,
            // vezi nota din docs/SEED_DATA.md 1.4: numele afișat rămâne cel real al beneficiarului legat,
            // inconsistența de transcriere din evidența originală nu e reprodusă ca snapshot separat)
            new("Jingă Florina", "unit-laborator-124", new(2026, 8, 3), new(2026, 9, 9), new(2026, 8, 3), new(2026, 9, 9),
                BookingStatus.Completed, "David", "Alexandra"),
            new("Bărculescu Claudia", "unit-laborator-124", new(2026, 7, 21), new(2026, 7, 23), new(2026, 7, 21), new(2026, 7, 23),
                BookingStatus.Completed, "David", "Alexandra"),
            new("Fam. Duroi", "unit-laborator-94", new(2026, 9, 6), new(2026, 9, 9), new(2026, 9, 6), new(2026, 9, 9),
                BookingStatus.Completed, "Ligia", "Alexandra"),
            new("Andrei Mihai", "unit-laborator-94", new(2026, 9, 1), new(2026, 9, 3), new(2026, 9, 1), new(2026, 9, 3),
                BookingStatus.Completed, "Ligia", "Alexandra"),
            new("Silochi Viorica", "unit-dristorului-893", new(2026, 7, 26), new(2026, 8, 27), new(2026, 7, 26), new(2026, 8, 27),
                BookingStatus.Completed, "Ștefan", "Alexandra"),
            new("Tirsoreanu Matilda", "unit-dristorului-893", new(2026, 7, 22), new(2026, 7, 24), new(2026, 7, 22), new(2026, 7, 24),
                BookingStatus.Completed, "Ștefan", "Alexandra"),
            new("Ovcearenco Victor", "unit-vlad-judetul", new(2026, 2, 9), new(2026, 6, 30), new(2026, 2, 9), new(2026, 6, 30),
                BookingStatus.Completed, "Alexandra", "Alexandra"),
            new("Mazilu Florin", "unit-traian-sufragerie", new(2026, 8, 13), new(2026, 8, 17), new(2026, 8, 13), new(2026, 8, 17),
                BookingStatus.Completed, "Cristi", "Alexandra"),
            new("Fam. Enache", "unit-stanescu-sufragerie", new(2026, 9, 2), new(2026, 9, 4), new(2026, 9, 2), new(2026, 9, 4),
                BookingStatus.Completed, "Miriam", "Alexandra"),
            new("Ivan Erik", "unit-elev-stefanescu", new(2025, 9, 27), new(2025, 10, 12), new(2025, 9, 27), new(2025, 10, 12),
                BookingStatus.Completed, "Alexandra", "Alexandra"),
            new("Chesa Dumitru", "unit-elev-stefanescu", new(2025, 10, 20), new(2025, 10, 22), new(2025, 10, 20), new(2025, 10, 22),
                BookingStatus.Completed, "Alexandra", "Alexandra"),
        ];

        var occupiedUnitKeys = new HashSet<string>();

        foreach (var spec in specs)
        {
            var beneficiary = beneficiaries[spec.BeneficiaryName];
            var unit = spec.UnitKey is not null ? units[spec.UnitKey] : null;
            var createdByUser = users[spec.RequestedByName];
            var decidedByUser = spec.DecidedByName is not null ? users[spec.DecidedByName] : null;

            // Timestamp-uri sintetice, derivate din datele reale (doar zi) — evidența originală
            // nu are ore exacte pentru creare/decizie, doar datele de check-in/check-out.
            var createdAt = spec.RequestedIn.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc).AddDays(-2);
            var decidedAt = decidedByUser is not null ? createdAt.AddHours(6) : (DateTime?)null;

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BeneficiaryId = beneficiary.Id,
                UnitId = unit?.Id,
                RequestedCheckIn = spec.RequestedIn,
                RequestedCheckOut = spec.RequestedOut,
                ActualCheckIn = spec.ActualIn,
                ActualCheckOut = spec.ActualOut,
                Status = spec.Status,
                CreatedByUserId = createdByUser.Id,
                CreatedAt = createdAt,
                DecidedByUserId = decidedByUser?.Id,
                DecidedAt = decidedAt,
                DecisionNote = spec.DecisionNote
            };
            db.Bookings.Add(booking);

            if (spec.Comment is { } comment)
            {
                db.BookingComments.Add(new BookingComment
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    AuthorUserId = users[comment.Author].Id,
                    Text = comment.Text,
                    CreatedAt = comment.Date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc)
                });
            }

            // Statusul unităților se derivă din cazările Active (vezi nota din SEED_DATA.md 1.2) —
            // nu ținut separat, ca să nu ajungă desincronizat.
            if (spec.Status == BookingStatus.Active && spec.UnitKey is not null)
            {
                occupiedUnitKeys.Add(spec.UnitKey);
            }
        }

        foreach (var key in occupiedUnitKeys)
        {
            units[key].Status = UnitStatus.Occupied;
        }
    }

    // ---- Curățenie & chei ----------------------------------------------------------------------

    private static void SeedCleaning(
        AppDbContext db, Dictionary<string, Property> properties, Dictionary<string, Unit> units,
        Dictionary<string, ApplicationUser> users, DateOnly today)
    {
        (string PropertyKey, string[] Volunteers, DayOfWeek Day)[] rotations =
        [
            ("prop-laborator-124", ["David", "Alina"], DayOfWeek.Sunday),
            ("prop-laborator-94", ["Ligia", "Ioana"], DayOfWeek.Thursday),
            ("prop-dristorului-893", ["Ștefan", "Alina", "Dominic", "Ema"], DayOfWeek.Saturday),
            ("prop-vlad-judetul", ["Sami", "Cristi"], DayOfWeek.Saturday),
            ("prop-traian-popovici", ["Rebeca", "Elena"], DayOfWeek.Saturday),
            ("prop-stanescu-gheorghe", ["Alexandra", "Miriam", "Florentina"], DayOfWeek.Saturday),
        ];

        foreach (var (propertyKey, volunteers, day) in rotations)
        {
            foreach (var volunteerName in volunteers)
            {
                db.CleaningAssignments.Add(new CleaningAssignment
                {
                    Id = Guid.NewGuid(),
                    PropertyId = properties[propertyKey].Id,
                    VolunteerUserId = users[volunteerName].Id,
                    ScheduledDayOfWeek = day
                });
            }
        }

        // Ture punctuale 🟡 — exemple, fără corespondent real în evidență.
        db.CleaningTasks.Add(new CleaningTask
        {
            Id = Guid.NewGuid(),
            UnitId = units["unit-laborator-124"].Id,
            ScheduledDate = today,
            Status = CleaningTaskStatus.Pending,
            Notes = "Verificare înainte de sosirea Danielei Adina (15/09)."
        });
        db.CleaningTasks.Add(new CleaningTask
        {
            Id = Guid.NewGuid(),
            UnitId = units["unit-dristorului-893"].Id,
            ScheduledDate = today.AddDays(-6),
            Status = CleaningTaskStatus.Done,
            CompletedByUserId = users["Ștefan"].Id,
            CompletedAt = today.AddDays(-6).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc)
        });
    }

    // ---- Mentenanță 🟡 (exemple, pe locații reale) -----------------------------------------------

    private static void SeedMaintenance(
        AppDbContext db, Dictionary<string, Property> properties, Dictionary<string, ApplicationUser> users, DateOnly today)
    {
        (string PropertyKey, MaintenanceTicketType Type, string Description, MaintenanceTicketPriority Priority,
            MaintenanceTicketStatus Status, decimal? EstimatedCost, decimal? ActualCost, string ReportedBy,
            string? AssignedTo, int CreatedDaysAgo, int? ResolvedDaysAgo)[] tickets =
        [
            ("prop-laborator-124", MaintenanceTicketType.Supplies, "Lipsesc becuri în baie.", MaintenanceTicketPriority.Low,
                MaintenanceTicketStatus.New, 40m, null, "David", null, 2, null),
            ("prop-dristorului-893", MaintenanceTicketType.Repair, "Robinet care picură în bucătărie.", MaintenanceTicketPriority.Medium,
                MaintenanceTicketStatus.Assigned, 120m, null, "Ligia", "Ștefan", 5, null),
            ("prop-elev-stefanescu", MaintenanceTicketType.Urgent, "Fără apă caldă.", MaintenanceTicketPriority.Urgent,
                MaintenanceTicketStatus.InProgress, 300m, null, "Alexandra", "Cristi", 1, null),
            ("prop-traian-popovici", MaintenanceTicketType.Repair, "Ușă dulap ieșită din balamale.", MaintenanceTicketPriority.Low,
                MaintenanceTicketStatus.Resolved, 30m, 25m, "Cristi", "Cristi", 8, 3),
        ];

        foreach (var t in tickets)
        {
            var createdAt = today.AddDays(-t.CreatedDaysAgo).ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
            db.MaintenanceTickets.Add(new MaintenanceTicket
            {
                Id = Guid.NewGuid(),
                PropertyId = properties[t.PropertyKey].Id,
                Type = t.Type,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                EstimatedCost = t.EstimatedCost,
                ActualCost = t.ActualCost,
                ReportedByUserId = users[t.ReportedBy].Id,
                CreatedAt = createdAt,
                AssignedToUserId = t.AssignedTo is not null ? users[t.AssignedTo].Id : null,
                ResolvedAt = t.ResolvedDaysAgo is { } daysAgo
                    ? today.AddDays(-daysAgo).ToDateTime(new TimeOnly(16, 0), DateTimeKind.Utc)
                    : null
            });
        }
    }

    // ---- Activități 🟡 (exemple, pe locații reale) ----------------------------------------------

    private static void SeedOpportunities(
        AppDbContext db, Dictionary<string, Property> properties, Dictionary<string, ApplicationUser> users, DateOnly today)
    {
        var cleaningOpportunity = new VolunteerOpportunity
        {
            Id = Guid.NewGuid(),
            Type = OpportunityType.Cleaning,
            Title = "Curățenie generală Ap. 124",
            ScheduledAt = today.AddDays(2).ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            HasTime = true,
            PropertyId = properties["prop-laborator-124"].Id,
            Capacity = 4
        };
        db.VolunteerOpportunities.Add(cleaningOpportunity);
        db.OpportunitySignups.Add(new OpportunitySignup { Id = Guid.NewGuid(), OpportunityId = cleaningOpportunity.Id, VolunteerUserId = users["David"].Id });

        var socialEvent = new VolunteerOpportunity
        {
            Id = Guid.NewGuid(),
            Type = OpportunityType.Event,
            Title = "Zi de socializare cu beneficiarii",
            ScheduledAt = today.AddDays(7).ToDateTime(new TimeOnly(16, 0), DateTimeKind.Utc),
            HasTime = true
        };
        db.VolunteerOpportunities.Add(socialEvent);
        db.OpportunitySignups.Add(new OpportunitySignup { Id = Guid.NewGuid(), OpportunityId = socialEvent.Id, VolunteerUserId = users["David"].Id });
        db.OpportunitySignups.Add(new OpportunitySignup { Id = Guid.NewGuid(), OpportunityId = socialEvent.Id, VolunteerUserId = users["Ligia"].Id });

        db.VolunteerOpportunities.Add(new VolunteerOpportunity
        {
            Id = Guid.NewGuid(),
            Type = OpportunityType.Visit,
            Title = "Vizită de evaluare locație nouă",
            ScheduledAt = today.AddDays(3).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            HasTime = false,
            Capacity = 2
        });

        db.VolunteerOpportunities.Add(new VolunteerOpportunity
        {
            Id = Guid.NewGuid(),
            Type = OpportunityType.Promotion,
            Title = "Stand de informare la eveniment caritabil",
            ScheduledAt = today.AddDays(12).ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc),
            HasTime = true,
            Capacity = 6
        });

        db.VolunteerOpportunities.Add(new VolunteerOpportunity
        {
            Id = Guid.NewGuid(),
            Type = OpportunityType.Event,
            Title = "Renovare mică la Ap. 32 — dată de stabilit",
            ScheduledAt = null,
            HasTime = false,
            PropertyId = properties["prop-elev-stefanescu"].Id
        });
    }

    // ---- Notificări 🟡 (exemple, relative la "azi") ---------------------------------------------

    private static void SeedNotifications(AppDbContext db, Dictionary<string, ApplicationUser> users, DateOnly today)
    {
        (string Recipient, NotificationType Type, string Message, bool IsRead, int DaysAgo)[] notifications =
        [
            ("Alexandra", NotificationType.NewBookingRequest, "Cerere nouă de cazare pentru Daniela Adina.", false, 1),
            ("Alexandra", NotificationType.NewBookingRequest, "Cerere nouă de cazare pentru Mama și copil.", false, 1),
            ("Alexandra", NotificationType.NewCommentOnBooking, "Comentariu nou la solicitarea lui Cotulbea Marian.", true, 10),
            ("Alexandra", NotificationType.NewMaintenanceTicket, "Sesizare nouă: Fără apă caldă.", false, 1),
            ("David", NotificationType.NewOpportunityPublished, "Activitate nouă: Zi de socializare cu beneficiarii.", true, 6),
            ("David", NotificationType.UnitNeedsCleaning, "Ap. 124 are nevoie de verificare înainte de următoarea cazare.", false, 0),
        ];

        foreach (var n in notifications)
        {
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = users[n.Recipient].Id,
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = today.AddDays(-n.DaysAgo).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc)
            });
        }
    }

    // ---- Configurare organizație --------------------------------------------------------------

    private static void SeedOrganizationSettings(AppDbContext db)
    {
        db.OrganizationSettings.Add(new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            StartDate = new DateOnly(2022, 1, 24),
            CurrentBalance = 0m,
            EstimatedMonthlyExpenses = 0m,
            ContactPhone = null,
            Announcement = null
        });
    }

    // ---- Box of Blessing — beneficiari ----------------------------------------------------------

    private static void SeedBobBeneficiaries(AppDbContext db)
    {
        (string FullName, string? Phone, BobMobility? Mobility, string? Address, string? Volunteer,
            BobBeneficiaryStatus Status, string? Notes)[] spec =
        [
            // Activi (10)
            ("Vintilă Alexandra", "0771.699.441", BobMobility.Deplasabil, "Str. Valea Ialomiței 6, Bl C10, Sc C, Et 10, Ap 192", "Sami", BobBeneficiaryStatus.Active, null),
            ("Szatmari Sanda", "0771.472.907", BobMobility.Nedeplasabil, "Moinești, Bl 127, Sc 2, Et 3, Ap 56", "Sami", BobBeneficiaryStatus.Active, null),
            ("Pleșca Adela Nicoleta", "0762.421.721", BobMobility.Deplasabil, "Str. Fabricii nr 2B-A, Bl 15D, Sc A, Et 6, Ap 32", "Cristi", BobBeneficiaryStatus.Active, null),
            ("Iliescu Gheorghe", "0770 778 981", BobMobility.Deplasabil, "Str. Ernest Juvara 31-33, Bl 1, Et 2, Ap 3", "Cristi", BobBeneficiaryStatus.Active, "0314308981 - fix"),
            ("Neacșu Marioara", "0769.171.653", BobMobility.Nedeplasabil, "Strada Straja 12, Bl.52, Sc.2, Et 7, Ap 108", "Sami", BobBeneficiaryStatus.Active, null),
            ("Marin Alexe", null, BobMobility.Deplasabil, "Strada Sergent Turturică 81", "George", BobBeneficiaryStatus.Active, null),
            ("Coporan Florian", "0760197138", BobMobility.Deplasabil, "Biserica Tuturor Sfinților Români", "George", BobBeneficiaryStatus.Active, null),
            ("Fam. cu copii - Ligia", null, null, null, "Ligia", BobBeneficiaryStatus.Active, null),
            ("Laurențiu Ierusalim", "0725157241", null, null, "Sami", BobBeneficiaryStatus.Active, null),
            ("Ștefănescu Rădița", "0770.256.557", BobMobility.Deplasabil, "Str. Rusetu 6, Bl G13, Sc 1, Et 1, Ap 6", "Sami", BobBeneficiaryStatus.Active, null),

            // Foști beneficiari (5)
            ("Mihăilă Smaranda", "021.745.33.11", BobMobility.Nedeplasabil, "Str. Latea Gheorghe 16, Bl C36, Et 5, Ap 64", null, BobBeneficiaryStatus.Former, null),
            ("Chiran Gheorghița", "0727.588.971", BobMobility.Deplasabil, "Str Partiturii 8, Bl 62, Sc 1, Et 4, Ap 20", null, BobBeneficiaryStatus.Former, null),
            ("Stoica Aurica", "0736.129.761", BobMobility.Nedeplasabil, "Drumul Cioroglarlei 147A", null, BobBeneficiaryStatus.Former, "pachet pentru cămin"),
            ("Ionescu Lucrețiu", "0733.433.427", BobMobility.Deplasabil, "Bd. 1 Mai nr. 26, Bl. 6S14, Sc. 1, Et 5, Ap. 62", null, BobBeneficiaryStatus.Former, null),
            ("Anghelina Costinel", "0726.901.119", BobMobility.Deplasabil, "Str. Topazului, Bragadiru", null, BobBeneficiaryStatus.Former, null),

            // Posibili beneficiari (2)
            ("Simionescu Catinca", "021.772.60.11", null, "Str. Răsăritului nr. 2, Bl. M9, Sc.1, Et. 5, Ap. 34", null, BobBeneficiaryStatus.Possible, null),
            ("Kurt Ștefania", "0764542668", null, "Intrarea Drumul la Roșu nr. 8", null, BobBeneficiaryStatus.Possible, null),
        ];

        foreach (var b in spec)
        {
            db.BobBeneficiaries.Add(new BobBeneficiary
            {
                Id = Guid.NewGuid(),
                FullName = b.FullName,
                Phone = b.Phone,
                Mobility = b.Mobility,
                Address = b.Address,
                AssignedVolunteerName = b.Volunteer,
                Status = b.Status,
                Contacted = false,
                Delivered = false,
                Notes = b.Notes
            });
        }
    }

    // ---- Box of Blessing — catalog cutie --------------------------------------------------------

    private static void SeedBobBox(AppDbContext db)
    {
        (string Key, string Title, (string Name, decimal Price, bool Checked)[] Items)[] categories =
        [
            ("bacanie", "Băcănie",
            [
                ("Făină", 1.69m, true), ("Zahăr", 3.79m, true), ("Sare", 2.75m, false), ("Ulei", 7.59m, true),
                ("Oțet", 3.49m, false), ("Mălai", 1.99m, true), ("Orez", 5.89m, false), ("Fasole", 3.50m, false),
                ("Griș", 3.19m, false),
            ]),
            ("conserve-nepreparate", "Conserve nepreparate",
            [
                ("Suc de roșii / Bulion", 5.49m, true), ("Mazăre", 4.00m, false), ("Fasole roșie", 3.45m, false),
                ("Fasole albă", 3.49m, true), ("Linte", 3.79m, false), ("Năut", 2.99m, false), ("Ciuperci", 5.99m, true),
                ("Porumb", 4.99m, false), ("Măsline", 3.89m, false), ("Castraveți murați", 5.59m, false),
                ("Varză murată", 5.30m, true), ("Sfeclă roșie", 7.59m, false), ("Fasole păstăi", 5.99m, false),
                ("Roșii pastă", 3.55m, false), ("Porumb (cutie mică)", 3.05m, false), ("Mix de legume", 8.49m, true),
            ]),
            ("conserve-preparate", "Conserve preparate",
            [
                ("Hering în suc de roșii", 6.09m, true), ("Hering în ulei", 4.95m, false), ("Sardine", 4.49m, false),
                ("Ton mărunțit", 4.95m, true), ("Pate vegetal (mic)", 2.20m, false), ("Tocană", 3.49m, true),
                ("Zacuscă", 3.99m, false), ("Pate", 6.99m, true), ("Fasole cu cârnăciori", 2.99m, false),
                ("Chiftelțe marinate", 11.19m, true), ("Ciorbă fasole afumătură", 9.99m, true), ("Iahnie de fasole", 7.29m, false),
                ("Pate vegetal", 3.99m, false), ("Pastă de măsline", 4.99m, false),
            ]),
            ("fainoase", "Făinoase",
            [
                ("Supă plic", 3.39m, false), ("Fidea", 2.49m, false), ("Spaghetti", 2.39m, false),
                ("Tăiței", 2.59m, true), ("Penne", 3.79m, true), ("Melcișori", 4.89m, false),
            ]),
            ("dulciuri", "Dulciuri",
            [
                ("Covrigi", 5.19m, true), ("Corn", 4.00m, false), ("Grisine", 3.49m, false), ("Biscuiți sărați", 3.99m, false),
                ("Biscuiți dulci", 1.19m, true), ("Croissant", 1.69m, false), ("Napolitane", 4.50m, false),
                ("Bomboane", 8.99m, false), ("Cozonac / Chec", 13.99m, false), ("Ciocolată", 4.95m, false),
                ("Prăjiturică", 1.39m, false), ("Compot piersici", 7.99m, false), ("Stafide", 5.99m, false),
            ]),
            ("condimente", "Condimente",
            [
                ("Cafea", 3.89m, false), ("Ceai", 4.19m, false), ("Piper", 5.99m, false),
                ("Boia", 3.95m, false), ("Busuioc", 3.95m, false), ("Cacao", 5.39m, false),
            ]),
            ("igiena-curatenie", "Igienă & curățenie",
            [
                ("Săpun", 5.99m, false), ("Pastă de dinți", 2.39m, false), ("Gel de duș", 4.79m, false),
                ("Șervețele", 1.00m, false), ("Detergent de vase", 5.05m, false), ("Domestos", 11.50m, false),
                ("Spirt", 5.20m, false),
            ]),
            ("diverse-perisabile", "Diverse & perisabile",
            [
                ("Suncă pui", 8.00m, false), ("Mezel", 12.00m, false), ("Lapte UHT", 4.75m, false),
                ("Brânză topită", 5.29m, false), ("Drojdie", 3.57m, false),
            ]),
        ];

        var sortOrder = 0;
        foreach (var (key, title, items) in categories)
        {
            var category = new BoxCategory { Id = Guid.NewGuid(), Key = key, Title = title, SortOrder = sortOrder++ };
            db.BoxCategories.Add(category);

            var itemSortOrder = 0;
            foreach (var (name, price, isChecked) in items)
            {
                db.BoxItems.Add(new BoxItem
                {
                    Id = Guid.NewGuid(), CategoryId = category.Id, Name = name, Price = price,
                    Checked = isChecked, SortOrder = itemSortOrder++
                });
            }
        }
    }
}
