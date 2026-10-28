
namespace TodoApp.Core;

/// <summary>
/// One tick of a <see cref="Routine"/>: proof the routine was done on one date.
/// </summary>
/// <remarks>
/// Part of the Routine aggregate. Only <see cref="Routine.Tick"/> can create one,
/// so every tick goes through the routine's rules.
/// </remarks>
public class Completion
{
    /// <summary>The tick's unique id.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>The day that was ticked. This is the date that counts, not the moment of the click.</summary>
    public DateOnly Date { get; private set; }
    /// <summary>When the user actually clicked, in UTC. For a late tick this is after <see cref="Date"/>.</summary>
    public DateTime CompletedAt { get; private set; }
    // For EF Core only: it needs an empty constructor to load rows from the database.
    private Completion()
    {
    }
    /// <summary>Creates a tick for <paramref name="date"/>.</summary>
    /// <param name="date">The day being ticked.</param>
    /// <remarks>
    /// It's <c>internal</c>, so only code inside TodoApp.Core (in practice, <see cref="Routine"/>)
    /// can create one. The API can't skip the routine's rules.
    /// </remarks>
    internal Completion(DateOnly date)
    {
        Date = date;
        CompletedAt = DateTime.UtcNow;
    }
}
