using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> Tasks => Set<TodoItem>();
    public DbSet<Routine> Routines => Set<Routine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Tasks_Priority", "\"Priority\" BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_Tasks_TimeNeedsDate", "\"DueTime\" IS NULL OR \"DueDate\" IS NOT NULL");
            t.HasCheckConstraint("CK_Tasks_TitleNotBlank", "length(trim(\"Title\")) > 0");
        });
        modelBuilder.Entity<Routine>(routine =>
        {
            routine.Ignore(r => r.Days);
            // The model property keeps the name "days" (so the database and migrations don't change),
            // but the value lives in the private field _days.
            routine.Property<List<DayOfWeek>>("days")
                .HasField("_days")
                .HasColumnName("Days")
                .HasConversion(
                    days => string.Join(",", days),
                    text => text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(d => Enum.Parse<DayOfWeek>(d))
                                .ToList());

            routine.OwnsMany(r => r.Completions, completion =>
            {
                completion.ToTable("Completions");
                completion.WithOwner().HasForeignKey("RoutineId");
                completion.HasKey(c => c.Id);
                completion.Property(c => c.Id).ValueGeneratedNever();
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
