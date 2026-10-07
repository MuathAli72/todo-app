using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> Tasks => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Tasks_Priority", "\"Priority\" BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_Tasks_TimeNeedsDate", "\"DueTime\" IS NULL OR \"DueDate\" IS NOT NULL");
            t.HasCheckConstraint("CK_Tasks_TitleNotBlank", "length(trim(\"Title\")) > 0");
        });
    }
}