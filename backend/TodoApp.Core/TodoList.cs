namespace TodoApp.Core;

/// <summary>All the tasks. Every change to a task goes through this class.</summary>
/// <remarks>
/// The API loads the list, calls one method here, then saves the list (see TodoListRepository).
/// The change methods return false when no task has the given id. The API turns that into a 404.
/// This class doesn't know about HTTP, so it answers with true or false, not with a status code.
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

    /// <summary>Removes the task with this id.</summary>
    /// <returns>true if it was removed. false if no task has that id.</returns>
    public bool DeleteTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            return false;
        }
        tasks.Remove(task);
        return true;
    }

    /// <summary>Marks the task as done.</summary>
    /// <returns>true if it was found. false if no task has that id.</returns>
    public bool CompleteTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            return false;
        }
        task.Complete();
        return true;
    }

    /// <summary>Marks the task as not done.</summary>
    /// <returns>true if it was found. false if no task has that id.</returns>
    public bool ReopenTask(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            return false;
        }
        task.Reopen();
        return true;
    }

    /// <summary>Changes the task's priority.</summary>
    /// <returns>true if it was found. false if no task has that id.</returns>
    /// <exception cref="ArgumentException">The priority isn't Low, Medium or High.</exception>
    public bool ChangePriority(Guid id, Priority newPriority)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            return false;
        }
        task.ChangePriority(newPriority);
        return true;
    }

    /// <summary>Sets or removes the task's due date and time.</summary>
    /// <returns>true if it was found. false if no task has that id.</returns>
    /// <exception cref="ArgumentException">A time was given without a date.</exception>
    public bool ChangeDueDate(Guid id, DateOnly? newDate, TimeOnly? newTime)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            return false;
        }
        task.ChangeDueDate(newDate, newTime);
        return true;
    }

    /// <summary>Renames the task.</summary>
    /// <returns>true if it was found. false if no task has that id.</returns>
    /// <exception cref="ArgumentException">The new title is empty or only spaces.</exception>
    public bool EditTitle(Guid id, string newTitle)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            return false;
        }
        task.EditTitle(newTitle);
        return true;
    }

    /// <summary>The tasks, read-only. Use the methods above to change them.</summary>
    public IReadOnlyList<TodoItem> Tasks => tasks.AsReadOnly();
}