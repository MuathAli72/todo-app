namespace TodoApp.Core;

/// <summary>
/// Something the user repeats on chosen days of the week, like "Gym" on Monday, Wednesday and Friday.
/// </summary>
/// <remarks>
/// This is an aggregate root: it owns its <see cref="Completions"/>.
/// Ticks only change through <see cref="Tick"/> and <see cref="Untick"/>, so the rules
/// (a scheduled day, not in the future, not too old) can't be skipped.
/// Methods that depend on the date take "today" as a parameter instead of reading the clock,
/// so tests can pick any day.
/// </remarks>
public class Routine
{
    /// <summary>How many days back a missed day can still be ticked.</summary>
    private const int LateWindowDays = 6;

    // Private lists, so outside code can't add or remove items directly.
    // Outside code gets the read-only Days and Completions below.
    private List<DayOfWeek> _days = new();
    private List<Completion> _completions = new();

    /// <summary>The routine's unique id.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>The name shown in the list. Never empty.</summary>
    public string Title { get; private set; }
    /// <summary>The days of the week it repeats on. At least one, no duplicates.</summary>
    public IReadOnlyList<DayOfWeek> Days => _days.AsReadOnly();
    /// <summary>An optional time of day, like 07:00. Only shown on screen; no rule uses it.</summary>
    public TimeOnly? Time { get; private set; }
    /// <summary>The day the routine was created. Never changes.</summary>
    public DateOnly CreatedOn { get; private set; }
    /// <summary>
    /// The day the current schedule counts from. Days before it are never "missed" and can't be ticked.
    /// </summary>
    /// <remarks>
    /// Starts as <see cref="CreatedOn"/> and moves to today when the days change (see <see cref="ChangeDays"/>).
    /// Without this, changing Monday to Tuesday would suddenly show past Tuesdays as missed.
    /// The cost: missed days from the old schedule disappear too. A full fix would keep a history
    /// of old schedules, which we don't have yet.
    /// </remarks>
    public DateOnly ScheduleStartsOn { get; private set; }
    /// <summary>Every tick, read-only. Use <see cref="Tick"/> and <see cref="Untick"/> to change them.</summary>
    public IReadOnlyList<Completion> Completions => _completions.AsReadOnly();

    // For EF Core only: it needs an empty constructor to load rows from the database.
    private Routine()
    {
        Title = "";
    }

    /// <summary>Creates a routine.</summary>
    /// <param name="title">The name. Can't be empty or only spaces.</param>
    /// <param name="days">The days it repeats on. Duplicates are removed.</param>
    /// <param name="time">An optional time of day, or null.</param>
    /// <param name="createdOn">Today's date. The schedule counts from here.</param>
    /// <exception cref="ArgumentException">The title is empty, or no days were picked.</exception>
    public Routine(string title, IEnumerable<DayOfWeek> days, TimeOnly? time, DateOnly createdOn)
    {
        CheckTitle(title);
        var dayList = CheckDays(days);
        Title = title;
        _days = dayList;
        Time = time;
        CreatedOn = createdOn;
        ScheduleStartsOn = createdOn;
    }

    /// <summary>True if <paramref name="date"/> falls on one of the routine's days.</summary>
    public bool IsScheduledOn(DateOnly date) => _days.Contains(date.DayOfWeek);
    /// <summary>True if <paramref name="date"/> is on or after <see cref="ScheduleStartsOn"/>.</summary>
    public bool ExistedOn(DateOnly date) => date >= ScheduleStartsOn;
    /// <summary>True if <paramref name="date"/> has been ticked.</summary>
    public bool IsDoneOn(DateOnly date) => _completions.Any(c => c.Date == date);

    /// <summary>Renames the routine.</summary>
    /// <exception cref="ArgumentException">The new title is empty or only spaces.</exception>
    public void EditTitle(string newTitle)
    {
        CheckTitle(newTitle);
        Title = newTitle;
    }

    /// <summary>Sets the time of day, or removes it with null.</summary>
    public void ChangeTime(TimeOnly? newTime)
    {
        Time = newTime;
    }

    /// <summary>Replaces the days it repeats on, and restarts tracking from <paramref name="today"/>.</summary>
    /// <param name="newDays">The new days. Duplicates are removed.</param>
    /// <param name="today">Today's date. It becomes the new <see cref="ScheduleStartsOn"/>.</param>
    /// <exception cref="ArgumentException">No days were picked.</exception>
    public void ChangeDays(IEnumerable<DayOfWeek> newDays, DateOnly today)
    {
        _days = CheckDays(newDays);
        ScheduleStartsOn = today;
    }

    /// <summary>Marks <paramref name="date"/> as done.</summary>
    /// <param name="date">The day to tick: today, or a missed day up to 6 days back.</param>
    /// <param name="today">Today's date, passed in so tests can choose it.</param>
    /// <exception cref="ArgumentException">
    /// The day isn't one of the routine's days, is in the future, is before
    /// <see cref="ScheduleStartsOn"/>, or is more than 6 days ago.
    /// </exception>
    /// <remarks>Ticking a day that's already done does nothing, so clicking twice is safe.</remarks>
    public void Tick(DateOnly date, DateOnly today)
    {
        if (!IsScheduledOn(date))
        {
            throw new ArgumentException($"{Title} isn't scheduled on that day.");
        }
        if (date > today)
        {
            throw new ArgumentException("You can't tick a day that hasn't happened yet.");
        }
        if (!ExistedOn(date))
        {
            throw new ArgumentException($"{Title} didn't exist yet on that day.");
        }
        if (date < today.AddDays(-LateWindowDays))
        {
            throw new ArgumentException("That day is too long ago to tick.");
        }
        // Already ticked: do nothing, instead of adding a second tick for the same day.
        if (IsDoneOn(date))
        {
            return;
        }
        _completions.Add(new Completion(date));
    }

    /// <summary>Removes the tick for <paramref name="date"/>, if there is one.</summary>
    /// <remarks>No rules here, so a wrong tick can always be undone. Unticking a day that isn't ticked does nothing.</remarks>
    public void Untick(DateOnly date)
    {
        _completions.RemoveAll(c => c.Date == date);
    }

    /// <summary>The days in the last 6 days (not counting today) that were scheduled but not ticked.</summary>
    /// <param name="today">Today's date.</param>
    /// <returns>The missed days, newest first.</returns>
    /// <remarks>
    /// Worked out every time, never stored. So ticking a day takes it off this list with no extra code.
    /// Days before <see cref="ScheduleStartsOn"/> are skipped.
    /// </remarks>
    public List<DateOnly> MissedDays(DateOnly today)
    {
        var missed = new List<DateOnly>();
        for (int i = 1; i <= LateWindowDays; i++)
        {
            var day = today.AddDays(-i);
            if (!IsScheduledOn(day))
            {
                continue;
            }
            if (!ExistedOn(day))
            {
                continue;
            }
            if (IsDoneOn(day))
            {
                continue;
            }
            missed.Add(day);
        }
        return missed;
    }

    // Used by the constructor and EditTitle, so the title rule lives in one place.
    private static void CheckTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Please enter a title.");
        }
    }

    // Removes duplicates, then makes sure at least one day is left. Used by the constructor and ChangeDays.
    private static List<DayOfWeek> CheckDays(IEnumerable<DayOfWeek> days)
    {
        var dayList = days.Distinct().ToList();
        if (dayList.Count == 0)
        {
            throw new ArgumentException("Pick at least one day.");
        }
        return dayList;
    }
}
