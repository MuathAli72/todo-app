namespace TodoApp.Core;

public class TodoItem
{
    public string Title { get; }
    public bool IsDone { get; private set; }

    
    public TodoItem(string title)
    {
        if (string.IsNullOrEmpty(title))
    {
        throw new ArgumentException("Title required");
    }
        Title = title;
        IsDone = false;
    }

    public void Complete()
    {
        IsDone = true;
    }

    public void Reopen()
    {
        IsDone = false;
    }


}