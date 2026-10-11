using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

public class RoutineRepository
{
    private readonly TodoDbContext db;

    public RoutineRepository(TodoDbContext db)
    {
        this.db = db;
    }

    public async Task<List<Routine>> GetAllAsync() =>
        await db.Routines.OrderBy(r => r.CreatedOn).ToListAsync();

    public async Task<Routine?> GetAsync(Guid id) =>
        await db.Routines.FirstOrDefaultAsync(r => r.Id == id);

    public void Add(Routine routine) => db.Routines.Add(routine);

    public void Remove(Routine routine) => db.Routines.Remove(routine);

    public async Task SaveAsync() => await db.SaveChangesAsync();
}