namespace TodoApp.Core;

public class TodoItem
{
    public string Title { get; private set; }
    public bool IsDone { get; private set; }
    public Priority Priority { get; private set; } = Priority.Medium;
    public DateTime? DueDate { get; private set; }
    public Guid Id { get; } = Guid.NewGuid();


    public TodoItem(string title)
    {
        if (string.IsNullOrEmpty(title))
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
        Priority = newPriority;
    }

    internal void ChangeDueDate(DateTime? newDate)
    {
        DueDate = newDate;
    }

    internal void EditTitle(string newTitle)
{
    if (string.IsNullOrEmpty(newTitle))
    {
        throw new ArgumentException("Title required");
    }
    Title = newTitle;
}


}