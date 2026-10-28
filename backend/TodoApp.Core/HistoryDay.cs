namespace TodoApp.Core;

/// <summary>What happened with a routine on one day.</summary>
public enum DayStatus
{
    /// <summary>The routine didn't exist yet.</summary>
    NotStarted,
    /// <summary>Not one of the routine's days (under the schedule valid back then).</summary>
    Off,
    /// <summary>Ticked.</summary>
    Done,
    /// <summary>A scheduled day that wasn't ticked.</summary>
    Missed,
    /// <summary>Today, scheduled, not ticked yet. Not missed: the day isn't over.</summary>
    Pending,
}

/// <summary>One day in a routine's history.</summary>
/// <param name="Date">The day.</param>
/// <param name="Status">What happened that day.</param>
/// <param name="CanTick">True for a missed day that's still inside the 6-day window, so it can be ticked late.</param>
public record HistoryDay(DateOnly Date, DayStatus Status, bool CanTick);
