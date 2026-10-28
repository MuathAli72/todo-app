using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TodoApp.Core;

/// <summary>
/// The database, as EF Core sees it: our tables, plus the user tables from ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// It extends <c>IdentityDbContext</c> instead of plain <c>DbContext</c>, so Identity's tables
/// (AspNetUsers and friends) live in the same Postgres database as the tasks and routines.
/// The rules here mirror the domain rules as database constraints, so bad data can't get in
/// even if something skips the backend.
/// <para>
/// <b>Every task and routine belongs to one user.</b> A hidden "UserId" column links each row to
/// its owner, and a global query filter adds "WHERE UserId = the logged-in user" to EVERY query
/// on Tasks and Routines, automatically. That's why the repositories never mention users.
/// To see every user's rows on purpose (e.g. an admin report), use <c>.IgnoreQueryFilters()</c>.
/// </para>
/// </remarks>
public class TodoDbContext : IdentityDbContext<IdentityUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>ASP.NET Core passes in the options (which database) and the current request.</summary>
    public TodoDbContext(DbContextOptions<TodoDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // The logged-in user's id, read fresh every time it's used (not once at the start),
    // because the login system can create this context before it knows who the user is.
    // null when nobody is logged in: then the filter matches no rows at all.
    private string? CurrentUserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>The Tasks table.</summary>
    public DbSet<TodoItem> Tasks => Set<TodoItem>();

    /// <summary>The Routines table. Each routine's ticks are in Completions, its schedules in RoutineSchedules.</summary>
    public DbSet<Routine> Routines => Set<Routine>();

    /// <summary>Each user's secret calendar link (for reminders in their phone's calendar).</summary>
    public DbSet<CalendarFeed> CalendarFeeds => Set<CalendarFeed>();

    /// <summary>Describes how the classes map to tables, plus the extra database rules.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity sets up its own tables here. Without this line, the user tables are broken.
        base.OnModelCreating(modelBuilder);

        // ----- Ownership: every task and routine belongs to one user -----
        // "UserId" is a shadow property: a database column the C# classes don't have, so the
        // domain (TodoApp.Core) stays free of login details. Deleting a user deletes their data.
        modelBuilder.Entity<TodoItem>(task =>
        {
            task.Property<string>("UserId").IsRequired();
            task.HasOne<IdentityUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
            task.HasIndex("UserId");
            // The global filter. Added to every query on Tasks.
            task.HasQueryFilter(t => EF.Property<string>(t, "UserId") == CurrentUserId);
        });
        modelBuilder.Entity<Routine>(routine =>
        {
            routine.Property<string>("UserId").IsRequired();
            routine.HasOne<IdentityUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
            routine.HasIndex("UserId");
            // The global filter. Added to every query on Routines (their ticks come along with them).
            routine.HasQueryFilter(r => EF.Property<string>(r, "UserId") == CurrentUserId);
        });

        // One secret calendar link per user. Looked up by its token, which must be unique.
        modelBuilder.Entity<CalendarFeed>(feed =>
        {
            feed.HasKey(f => f.UserId);
            feed.HasIndex(f => f.Token).IsUnique();
            feed.HasOne<IdentityUser>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TodoItem>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Tasks_Priority", "\"Priority\" BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_Tasks_TimeNeedsDate", "\"DueTime\" IS NULL OR \"DueDate\" IS NOT NULL");
            t.HasCheckConstraint("CK_Tasks_TitleNotBlank", "length(trim(\"Title\")) > 0");
        });
        modelBuilder.Entity<Routine>(routine =>
        {
            // Days are stored as one text column, like "Monday,Wednesday,Friday".
            routine.Ignore(r => r.Days);
            // The days live in the private field _days. EF needs the field's exact name,
            // and HasColumnName keeps the database column called "Days".
            // Tells EF how to compare two day lists. Without it, EF only notices when the list
            // is swapped for a new one, not when its contents change (warning 10620).
            var daysComparer = new ValueComparer<List<DayOfWeek>>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                list => list.Aggregate(0, (hash, day) => HashCode.Combine(hash, day)),
                list => list.ToList());

            routine.Property<List<DayOfWeek>>("_days")
                .HasColumnName("Days")
                .HasConversion(
                    days => string.Join(",", days),
                    text => text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(d => Enum.Parse<DayOfWeek>(d))
                                .ToList(),
                    daysComparer);

            // Every schedule the routine has had, so past days are judged by the days valid back then.
            // Like ticks, they belong to their routine: loaded, saved and deleted with it.
            routine.OwnsMany(r => r.Schedules, schedule =>
            {
                schedule.ToTable("RoutineSchedules");
                schedule.WithOwner().HasForeignKey("RoutineId");
                schedule.HasKey(s => s.Id);
                schedule.Property(s => s.Id).ValueGeneratedNever();
                schedule.Ignore(s => s.Days);
                // Same storage as the routine's own days: one text column like "Monday,Friday".
                schedule.Property<List<DayOfWeek>>("_days")
                    .HasColumnName("Days")
                    .HasConversion(
                        days => string.Join(",", days),
                        text => text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(d => Enum.Parse<DayOfWeek>(d))
                                    .ToList(),
                        daysComparer);
            });

            // Ticks belong to their routine: loaded, saved and deleted with it.
            routine.OwnsMany(r => r.Completions, completion =>
            {
                completion.ToTable("Completions");
                completion.WithOwner().HasForeignKey("RoutineId");
                completion.HasKey(c => c.Id);
                completion.Property(c => c.Id).ValueGeneratedNever();
                // One tick per routine per day, even if two requests arrive at once.
                completion.HasIndex("RoutineId", "Date").IsUnique();
            });

            routine.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Routines_TitleNotBlank", "length(trim(\"Title\")) > 0");
                t.HasCheckConstraint("CK_Routines_HasDays", "\"Days\" <> ''");
            });
        });
    }

    /// <summary>Saves changes, first stamping new tasks and routines with the logged-in user.</summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampOwner();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>Saves changes, first stamping new tasks and routines with the logged-in user.</summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampOwner();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // New rows get the current user as their owner, so no endpoint can forget to set it.
    private void StampOwner()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is TodoItem or Routine)
            {
                entry.Property("UserId").CurrentValue = CurrentUserId
                    ?? throw new InvalidOperationException("Can't save a task or routine without a logged-in user.");
            }
        }
    }
}
