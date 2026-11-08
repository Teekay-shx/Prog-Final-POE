using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Users ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Users_Role",
                $"[Role] IN ('{UserRole.Admin}', '{UserRole.Organiser}', '{UserRole.Participant}')"));
        });

        // ---------- Events ----------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Events_Status",
                    $"[Status] IN ('{EventStatus.Planned}', '{EventStatus.Open}', '{EventStatus.Closed}', '{EventStatus.Completed}', '{EventStatus.Cancelled}')");
                t.HasCheckConstraint("CK_Events_Type",
                    $"[Type] IN ('{Models.EventType.Run}', '{Models.EventType.Walk}', '{Models.EventType.Cycle}')");
            });
        });

        // ---------- Categories ----------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Enrolments ----------
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasOne(en => en.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(en => en.ParticipantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(en => new { en.ParticipantId, en.CategoryId }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status",
                $"[Status] IN ('{EnrolmentStatus.Registered}', '{EnrolmentStatus.CheckedIn}', '{EnrolmentStatus.Cancelled}')"));
        });

        // ---------- Results (1:0..1 with Enrolment) ----------
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasOne(r => r.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.EnrolmentId).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("CK_Results_Status",
                $"[Status] IN ('{ResultStatus.Finished}', '{ResultStatus.DNF}', '{ResultStatus.DSQ}')"));
        });

        // ---------- Payments (1:0..1 with Enrolment) ----------
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasOne(p => p.Enrolment)
                  .WithOne(en => en.Payment)
                  .HasForeignKey<Payment>(p => p.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.EnrolmentId).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("CK_Payments_Status",
                $"[Status] IN ('{PaymentStatus.Pending}', '{PaymentStatus.Completed}', '{PaymentStatus.Refunded}', '{PaymentStatus.Failed}')"));
        });
    }
}
