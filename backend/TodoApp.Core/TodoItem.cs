namespace TodoApp.Core;

public class TodoItem
{
    public string Title { get; private set; }
    public bool IsDone { get; private set; }
    public Priority Priority { get; private set; } = Priority.Medium;
    public DateOnly? DueDate { get; private set; }
    public TimeOnly? DueTime { get; private set; }
    public Guid Id { get; } = Guid.NewGuid();


    public TodoItem(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title required");
        }
        Title = title;
        IsDone = false;
    }

    internal void Complete()
    {
        IsDone = true;
    }

    internal void Reopen()
    {
        IsDone = false;
    }

    internal void ChangePriority(Priority newPriority)
    {
        if (!Enum.IsDefined(newPriority))
        {
            throw new ArgumentException("Priority must be low, medium or high.");
        }
        Priority = newPriority;
    }
    internal void ChangeDueDate(DateOnly? newDate, TimeOnly? newTime)
    {
        if (newDate == null && newTime != null)
        {
            throw new ArgumentException ( "A time needs a date");
        }
        DueDate = newDate;
        DueTime = newTime;
    }

    internal void EditTitle(string newTitle)
{
    if (string.IsNullOrWhiteSpace(newTitle))
    {
        throw new ArgumentException("Title required");
    }
    Title = newTitle;
}


}