using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

/// <summary>Loads and saves routines, with their ticks, in the database.</summary>
/// <remarks>
/// Each request follows the same steps: load a routine, call its methods, then <see cref="SaveAsync"/>.
/// EF Core keeps track of what it loaded, so SaveAsync only writes what changed.
/// There's no repository for Completion: ticks belong to their routine (the aggregate),
/// so they're always loaded and saved with it.
/// </remarks>
public class RoutineRepository
{
    private readonly TodoDbContext _db;

    /// <summary>Creates the repository. ASP.NET Core passes in the database context for the current request.</summary>
    public RoutineRepository(TodoDbContext db)
    {
        this._db = db;
    }

    /// <summary>Loads every routine with its ticks, oldest first.</summary>
    public async Task<List<Routine>> GetAllAsync() =>
        await _db.Routines.OrderBy(r => r.CreatedOn).ToListAsync();

    /// <summary>Loads one routine with its ticks.</summary>
    /// <returns>The routine, or null if no routine has that id. The API turns null into 404.</returns>
    public async Task<Routine?> GetAsync(Guid id) =>
        await _db.Routines.FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>Marks a new routine to be inserted on the next save.</summary>
    public void Add(Routine routine) => _db.Routines.Add(routine);

    /// <summary>Marks a routine, and its ticks, to be deleted on the next save.</summary>
    public void Remove(Routine routine) => _db.Routines.Remove(routine);

    /// <summary>Writes every change since loading to the database, in one go.</summary>
    public async Task SaveAsync() => await _db.SaveChangesAsync();
}
