using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

public class TodoListRepository
{
    private readonly TodoDbContext db;

    public TodoListRepository(TodoDbContext db)
    {
        this.db = db;
    }

    public async Task<TodoList> LoadAsync()
    {
        var tasks = await db.Tasks.OrderBy(t => t.CreatedAt).ToListAsync();
        return new TodoList(tasks);
    }

    public async Task SaveAsync(TodoList list)
    {
        foreach (var task in list.Tasks)
        {
            if (db.Entry(task).State == EntityState.Detached)
            {
                db.Tasks.Add(task);
            }
        }

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