using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

/// <summary>Loads and saves the task list in the database.</summary>
/// <remarks>
/// Each request follows the same steps: <see cref="LoadAsync"/>, call one <see cref="TodoList"/> method,
/// then <see cref="SaveAsync"/>.
/// </remarks>
public class TodoListRepository
{
    private readonly TodoDbContext db;

    /// <summary>Creates the repository. ASP.NET Core passes in the database context for the current request.</summary>
    public TodoListRepository(TodoDbContext db)
    {
        this.db = db;
    }

    /// <summary>Loads every task, oldest first, as one <see cref="TodoList"/>.</summary>
    /// <remarks>Simple, but it loads all tasks even to change one. Fine for one person's list.</remarks>
    public async Task<TodoList> LoadAsync()
    {
        var tasks = await db.Tasks.OrderBy(t => t.CreatedAt).ToListAsync();
        return new TodoList(tasks);
    }

    /// <summary>Saves the list: new tasks are inserted, removed ones are deleted, edited ones are updated.</summary>
    /// <param name="list">The list from <see cref="LoadAsync"/>, after the change.</param>
    /// <remarks>
    /// TodoList doesn't know the database exists, so it can't tell EF what it added or removed.
    /// This method works that out by comparing the list with what EF loaded.
    /// Edits need no code here: EF already tracks the loaded tasks and notices changed values.
    /// </remarks>
    public async Task SaveAsync(TodoList list)
    {
        // A task EF has never seen ("Detached") was just made by AddTask, so insert it.
        foreach (var task in list.Tasks)
        {
            if (db.Entry(task).State == EntityState.Detached)
            {
                db.Tasks.Add(task);
            }
        }

        // A task EF loaded (db.Tasks.Local) that's no longer in the list was deleted, so delete its row.
        // ToList() makes a copy first, because you can't remove items from a collection while looping over it.
        foreach (var task in db.Tasks.Local.ToList())
        {
            if (!list.Tasks.Contains(task))
            {
                db.Tasks.Remove(task);
            }
        }

        await db.SaveChangesAsync();
    }
}