using System.Text.Json;
using Emaus.Domain.Bob;
using Emaus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Emaus.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Locality> Localities => Set<Locality>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingComment> BookingComments => Set<BookingComment>();
    public DbSet<CleaningAssignment> CleaningAssignments => Set<CleaningAssignment>();
    public DbSet<CleaningTask> CleaningTasks => Set<CleaningTask>();
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    public DbSet<VolunteerOpportunity> VolunteerOpportunities => Set<VolunteerOpportunity>();
    public DbSet<OpportunitySignup> OpportunitySignups => Set<OpportunitySignup>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserPushToken> UserPushTokens => Set<UserPushToken>();
    public DbSet<RequestLog> RequestLogs => Set<RequestLog>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();

    // ---- Box of Blessing (BOB) — vezi Emaus.Domain/Bob, docs/API.md §13 --------------------
    public DbSet<BobBeneficiary> BobBeneficiaries => Set<BobBeneficiary>();
    public DbSet<BoxCategory> BoxCategories => Set<BoxCategory>();
    public DbSet<BoxItem> BoxItems => Set<BoxItem>();
    public DbSet<BobDeliveryRecord> BobDeliveryRecords => Set<BobDeliveryRecord>();
    public DbSet<BobDeliveryRecordItem> BobDeliveryRecordItems => Set<BobDeliveryRecordItem>();
    public DbSet<BobSettings> BobSettings => Set<BobSettings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---- Utilizatori — cererile de acces în așteptare ------------------------------------
        b.Entity<ApplicationUser>().HasIndex(u => u.Status);

        // ---- Property / Unit --------------------------------------------------
        b.Entity<Property>()
            .HasMany(p => p.Units)
            .WithOne(u => u.Property)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Unit>()
            .HasIndex(u => new { u.PropertyId, u.Name })
            .IsUnique();

        // Listă simplă de nume (nu o legătură FK) — stocată ca JSON într-o singură coloană text,
        // cu ValueComparer explicit ca EF Core să detecteze corect schimbările din listă
        // (fără el, mutarea unui element în listă nu ar fi văzută ca modificare de SaveChanges).
        b.Entity<Property>()
            .Property(p => p.KeyHolders)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b2) => (a ?? new()).SequenceEqual(b2 ?? new()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        // ---- Beneficiary --------------------------------------------------------
        b.Entity<Beneficiary>()
            .HasOne(ben => ben.Locality)
            .WithMany(l => l.Beneficiaries)
            .HasForeignKey(ben => ben.LocalityId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<Beneficiary>()
            .HasIndex(ben => ben.Phone);

        // ---- Booking --------------------------------------------------------------
        b.Entity<Booking>()
            .HasOne(bk => bk.Beneficiary)
            .WithMany(ben => ben.Bookings)
            .HasForeignKey(bk => bk.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Booking>()
            .HasOne(bk => bk.Unit)
            .WithMany(u => u.Bookings)
            .HasForeignKey(bk => bk.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Booking>()
            .HasOne(bk => bk.CreatedByUser)
            .WithMany(usr => usr.BookingsRequested)
            .HasForeignKey(bk => bk.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Booking>()
            .HasOne(bk => bk.DecidedByUser)
            .WithMany(usr => usr.BookingsDecided)
            .HasForeignKey(bk => bk.DecidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Booking>()
            .HasOne(bk => bk.CaseManagerUser)
            .WithMany()
            .HasForeignKey(bk => bk.CaseManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Nu permite două cazări active suprapuse pe aceeași unitate — verificare
        // suplimentară de făcut și în serviciul de aplicație (EF Core nu poate
        // exprima "fără interval suprapus" direct printr-un index).
        b.Entity<Booking>()
            .HasIndex(bk => new { bk.UnitId, bk.Status });

        b.Entity<BookingComment>()
            .HasOne(c => c.Booking)
            .WithMany(bk => bk.Comments)
            .HasForeignKey(c => c.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Cleaning ---------------------------------------------------------------
        b.Entity<CleaningAssignment>()
            .HasOne(ca => ca.Property)
            .WithMany(p => p.CleaningAssignments)
            .HasForeignKey(ca => ca.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CleaningTask>()
            .HasOne(ct => ct.Unit)
            .WithMany(u => u.CleaningTasks)
            .HasForeignKey(ct => ct.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Maintenance --------------------------------------------------------------
        b.Entity<MaintenanceTicket>()
            .HasOne(t => t.Property)
            .WithMany(p => p.MaintenanceTickets)
            .HasForeignKey(t => t.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<MaintenanceTicket>()
            .HasOne(t => t.ReportedByUser)
            .WithMany()
            .HasForeignKey(t => t.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<MaintenanceTicket>()
            .HasOne(t => t.AssignedToUser)
            .WithMany(usr => usr.MaintenanceTicketsAssigned)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Volunteer opportunities -------------------------------------------------------
        b.Entity<VolunteerOpportunity>()
            .HasOne(o => o.Property)
            .WithMany()
            .HasForeignKey(o => o.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<OpportunitySignup>()
            .HasOne(s => s.Opportunity)
            .WithMany(o => o.Signups)
            .HasForeignKey(s => s.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<OpportunitySignup>()
            .HasIndex(s => new { s.OpportunityId, s.VolunteerUserId })
            .IsUnique();

        // ---- Notifications --------------------------------------------------------------
        b.Entity<Notification>()
            .HasOne(n => n.RecipientUser)
            .WithMany(usr => usr.Notifications)
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Notification>()
            .HasIndex(n => new { n.RecipientUserId, n.IsRead });

        // ---- Push notifications (FCM) -----------------------------------------------------
        b.Entity<UserPushToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unic global, nu per utilizator — un token aparține unui singur dispozitiv/instalare;
        // dacă apare din nou legat de alt user (device resetat, alt login), se reasignează,
        // nu se duplică (vezi NotificationService.RegisterPushTokenAsync).
        b.Entity<UserPushToken>().HasIndex(t => t.Token).IsUnique();

        // ---- Request log ------------------------------------------------------------------
        // Ordonate mereu după dată descrescător în UI — indexul reflectă exact tiparul de interogare.
        b.Entity<RequestLog>()
            .HasIndex(l => l.CreatedAt);

        // ---- Money fields -----------------------------------------------------------------
        b.Entity<Beneficiary>().Property(x => x.AccountBalance).HasPrecision(10, 2);
        b.Entity<MaintenanceTicket>().Property(x => x.EstimatedCost).HasPrecision(10, 2);
        b.Entity<MaintenanceTicket>().Property(x => x.ActualCost).HasPrecision(10, 2);
        b.Entity<OrganizationSettings>().Property(x => x.CurrentBalance).HasPrecision(10, 2);
        b.Entity<OrganizationSettings>().Property(x => x.EstimatedMonthlyExpenses).HasPrecision(10, 2);

        // ---- Box of Blessing (BOB) — vezi Emaus.Domain/Bob, docs/API.md §13 ------------------
        b.Entity<BoxCategory>()
            .HasMany(c => c.Items)
            .WithOne(i => i.Category)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BoxCategory>().HasIndex(c => c.Key).IsUnique();

        b.Entity<BobDeliveryRecord>()
            .HasMany(r => r.Items)
            .WithOne(i => i.DeliveryRecord)
            .HasForeignKey(i => i.DeliveryRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BoxItem>().Property(x => x.Price).HasPrecision(10, 2);
        b.Entity<BobDeliveryRecord>().Property(x => x.Total).HasPrecision(10, 2);
        b.Entity<BobDeliveryRecordItem>().Property(x => x.Price).HasPrecision(10, 2);
        b.Entity<BobSettings>().Property(x => x.MaxBudgetPerBox).HasPrecision(10, 2);
    }
}
