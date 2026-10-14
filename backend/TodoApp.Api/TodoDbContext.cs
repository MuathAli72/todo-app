using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

/// <summary>
/// The database, as EF Core sees it: our tables, plus the user tables from ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// It extends <c>IdentityDbContext</c> instead of plain <c>DbContext</c>, so Identity's tables
/// (AspNetUsers and friends) live in the same Postgres database as the tasks and routines.
/// The rules here mirror the domain rules as database constraints, so bad data can't get in
/// even if something skips the backend.
/// </remarks>
public class TodoDbContext : IdentityDbContext<IdentityUser>
{
    /// <summary>ASP.NET Core passes in the options (which database, the connection string).</summary>
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    /// <summary>The Tasks table.</summary>
    public DbSet<TodoItem> Tasks => Set<TodoItem>();

    /// <summary>The Routines table. Each routine's ticks are in the Completions table.</summary>
    public DbSet<Routine> Routines => Set<Routine>();

    /// <summary>Describes how the classes map to tables, plus the extra database rules.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity sets up its own tables here. Without this line, the user tables are broken.
        base.OnModelCreating(modelBuilder);

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
            routine.Property<List<DayOfWeek>>("days")
                .HasColumnName("Days")
                .HasConversion(
                    days => string.Join(",", days),
                    text => text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(d => Enum.Parse<DayOfWeek>(d))
                                .ToList());

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
}