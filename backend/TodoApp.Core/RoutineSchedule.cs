namespace TodoApp.Core;

/// <summary>
/// One version of a routine's schedule: which days it repeated on, starting from a date.
/// </summary>
/// <remarks>
/// Part of the Routine aggregate. Every time the days change, a new schedule starts, and the old
/// ones are kept. So "was Monday a gym day back then?" is answered with the schedule that was valid
/// on that Monday, not today's. That keeps missed days, history and streaks honest after a change.
/// </remarks>
public class RoutineSchedule
{
    private List<DayOfWeek> _days = new();

    /// <summary>The schedule's unique id.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>The first day this schedule applies to. It applies until the next schedule starts.</summary>
    public DateOnly StartsOn { get; private set; }
    /// <summary>The days of the week it repeats on.</summary>
    public IReadOnlyList<DayOfWeek> Days => _days.AsReadOnly();

    // For EF Core only: it needs an empty constructor to load rows from the database.
    private RoutineSchedule()
    {
    }

    /// <summary>Only <see cref="Routine"/> creates schedules, after checking the days.</summary>
    internal RoutineSchedule(DateOnly startsOn, List<DayOfWeek> days)
    {
        StartsOn = startsOn;
        _days = days;
    }

    /// <summary>Changes the days of a schedule that starts today (two changes on one day become one).</summary>
    internal void ReplaceDays(List<DayOfWeek> days)
    {
        _days = days;
    }
}
