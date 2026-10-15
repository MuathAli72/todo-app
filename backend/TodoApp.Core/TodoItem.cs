namespace TodoApp.Core;

/// <summary>One task in the to-do list.</summary>
/// <remarks>
/// The change methods are <c>internal</c>, so the API can't call them directly.
/// Every change goes through <see cref="TodoList"/>.
/// </remarks>
public class TodoItem
{
    /// <summary>The task's text. Never empty.</summary>
    public string Title { get; private set; }
    /// <summary>True once the task is ticked.</summary>
    public bool IsDone { get; private set; }
    /// <summary>Low, Medium or High. New tasks start at Medium.</summary>
    public Priority Priority { get; private set; } = Priority.Medium;
    /// <summary>The day it's due, or null for no due date.</summary>
    public DateOnly? DueDate { get; private set; }
    /// <summary>The time it's due, or null. Only allowed when <see cref="DueDate"/> is set.</summary>
    public TimeOnly? DueTime { get; private set; }
    /// <summary>The task's unique id.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>When the task was created, in UTC. The list is sorted by this.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // For EF Core only: it needs an empty constructor to load rows from the database.
    private TodoItem()
    {
        Title = "";
    }
    /// <summary>Creates a task that isn't done yet.</summary>
    /// <param name="title">The task's text. Can't be empty or only spaces.</param>
    /// <exception cref="ArgumentException">The title is empty.</exception>
    public TodoItem(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title required");
        }
        Title = title;
        IsDone = false;
    }

    /// <summary>Marks the task as done.</summary>
    internal void Complete()
    {
        IsDone = true;
    }

    /// <summary>Marks the task as not done.</summary>
    internal void Reopen()
    {
        IsDone = false;
    }

    /// <summary>Sets the priority.</summary>
    /// <exception cref="ArgumentException">The value isn't Low, Medium or High.</exception>
    internal void ChangePriority(Priority newPriority)
    {
        // Enum.IsDefined catches values like (Priority)7, which C# would otherwise allow.
        if (!Enum.IsDefined(newPriority))
        {
            throw new ArgumentException("Priority must be low, medium or high.");
        }
        Priority = newPriority;
    }
    /// <summary>Sets the due date and time. Pass null for both to remove them.</summary>
    /// <param name="newDate">The due day, or null.</param>
    /// <param name="newTime">The due time, or null. It needs a date.</param>
    /// <exception cref="ArgumentException">A time was given without a date.</exception>
    internal void ChangeDueDate(DateOnly? newDate, TimeOnly? newTime)
    {
        if (newDate == null && newTime != null)
        {
            throw new ArgumentException("A time needs a date");
        }
        DueDate = newDate;
        DueTime = newTime;
    }

    /// <summary>Renames the task.</summary>
    /// <exception cref="ArgumentException">The new title is empty or only spaces.</exception>
    internal void EditTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            throw new ArgumentException("Title required");
        }
        Title = newTitle;
    }


}
