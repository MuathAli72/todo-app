namespace TodoApp.Core;

public class TodoList
{
    private List<TodoItem> tasks = new();

    public TodoItem AddTask(string title)
    {
        var task = new TodoItem(title);
        tasks.Add(task);
        return task;
    }

    public void DeleteTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            tasks.Remove(task);
        }
    }

    public void CompleteTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Complete();
        }
    }

    public void ReopenTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Reopen();
        }
    }

    public void ChangePriority(Guid id, Priority newPriority)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.ChangePriority(newPriority);
        }
    }

    public void ChangeDueDate(Guid id, DateTime? newDate)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.ChangeDueDate(newDate);
        }
    }

    public IReadOnlyList<TodoItem> Tasks => tasks.AsReadOnly();
}