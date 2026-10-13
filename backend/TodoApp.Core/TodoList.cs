namespace TodoApp.Core;

/// <summary>All the tasks. Every change to a task goes through this class.</summary>
/// <remarks>
/// The API loads the list, calls one method here, then saves the list (see TodoListRepository).
/// If no task has the given id, the change methods do nothing and don't throw.
/// </remarks>
public class TodoList
{
    // Private, so tasks can only be added or removed through the methods below.
    private List<TodoItem> tasks = new();

    /// <summary>Creates an empty list.</summary>
    public TodoList()
    {
    }

    /// <summary>Creates a list from tasks that already exist, like the ones loaded from the database.</summary>
    /// <param name="existingTasks">The tasks to start with. They're copied into a new list.</param>
    public TodoList(IEnumerable<TodoItem> existingTasks)
    {
        tasks = existingTasks.ToList();
    }

    /// <summary>Creates a task and adds it to the list.</summary>
    /// <param name="title">The task's text.</param>
    /// <returns>The new task, so the API can send it back with its id.</returns>
    /// <exception cref="ArgumentException">The title is empty.</exception>
    public TodoItem AddTask(string title)
    {
        var task = new TodoItem(title);
        tasks.Add(task);
        return task;
    }

    /// <summary>Removes the task with this id. Does nothing if the id isn't found.</summary>
    public void DeleteTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            tasks.Remove(task);
        }
    }

    /// <summary>Marks the task as done. Does nothing if the id isn't found.</summary>
    public void CompleteTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Complete();
        }
    }

    /// <summary>Marks the task as not done. Does nothing if the id isn't found.</summary>
    public void ReopenTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Reopen();
        }
    }

    /// <summary>Changes the task's priority. Does nothing if the id isn't found.</summary>
    /// <exception cref="ArgumentException">The priority isn't Low, Medium or High.</exception>
    public void ChangePriority(Guid id, Priority newPriority)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.ChangePriority(newPriority);
        }
    }

    /// <summary>Sets or removes the task's due date and time. Does nothing if the id isn't found.</summary>
    /// <exception cref="ArgumentException">A time was given without a date.</exception>
    public void ChangeDueDate(Guid id, DateOnly? newDate, TimeOnly? newTime)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.ChangeDueDate(newDate, newTime);
        }
    }

    /// <summary>Renames the task. Does nothing if the id isn't found.</summary>
    /// <exception cref="ArgumentException">The new title is empty or only spaces.</exception>
    public void EditTitle(Guid id, string newTitle)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.EditTitle(newTitle);
        }
    }

    /// <summary>The tasks, read-only. Use the methods above to change them.</summary>
    public IReadOnlyList<TodoItem> Tasks => tasks.AsReadOnly();
}