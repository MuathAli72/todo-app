namespace TodoApp.Core;

public class TodoItem
{
    public string Title { get; }
    public bool IsDone { get; private set; }

    public Priority Priority { get; private set; } = Priority.Medium;
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

    public void ChangePriority(Priority newPriority)
    {
        Priority = newPriority;
    }


}