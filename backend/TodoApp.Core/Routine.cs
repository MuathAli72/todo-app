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
    private List<RoutineSchedule> _schedules = new();

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
    /// <summary>The day the current schedule (the current <see cref="Days"/>) started.</summary>
    /// <remarks>
    /// Kept for display and for routines saved before schedule history existed.
    /// The rules use <see cref="Schedules"/>, which remembers every past schedule.
    /// </remarks>
    public DateOnly ScheduleStartsOn { get; private set; }
    /// <summary>Every tick, read-only. Use <see cref="Tick"/> and <see cref="Untick"/> to change them.</summary>
    public IReadOnlyList<Completion> Completions => _completions.AsReadOnly();
    /// <summary>
    /// Every schedule the routine has had, oldest first. A new one starts each time the days change,
    /// so past days are judged by the days that applied back then.
    /// </summary>
    public IReadOnlyList<RoutineSchedule> Schedules => _schedules.AsReadOnly();

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
        _schedules.Add(new RoutineSchedule(createdOn, dayList));
    }

    /// <summary>
    /// True if <paramref name="date"/> was one of the routine's days, under the schedule valid on that date.
    /// </summary>
    public bool IsScheduledOn(DateOnly date) => DaysOn(date).Contains(date.DayOfWeek);
    /// <summary>True if the routine existed on <paramref name="date"/> (on or after <see cref="CreatedOn"/>).</summary>
    public bool ExistedOn(DateOnly date) => date >= CreatedOn;
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

    /// <summary>
    /// The days the routine repeated on, on <paramref name="date"/>: from the schedule valid that day.
    /// Empty before the routine existed.
    /// </summary>
    public IReadOnlyList<DayOfWeek> DaysOn(DateOnly date)
    {
        if (!ExistedOn(date))
        {
            return Array.Empty<DayOfWeek>();
        }
        // The newest schedule that had already started on that date.
        var schedule = _schedules
            .Where(s => s.StartsOn <= date)
            .OrderByDescending(s => s.StartsOn)
            .FirstOrDefault();
        if (schedule != null)
        {
            return schedule.Days;
        }
        // A routine saved before schedule history existed (no schedules loaded): the old rule.
        return date >= ScheduleStartsOn ? _days : Array.Empty<DayOfWeek>();
    }

    /// <summary>Replaces the days it repeats on, from <paramref name="today"/> onwards.</summary>
    /// <param name="newDays">The new days. Duplicates are removed.</param>
    /// <param name="today">Today's date. The new schedule starts here; older days keep their old schedule.</param>
    /// <exception cref="ArgumentException">No days were picked.</exception>
    /// <remarks>
    /// Past ticks, missed days and streaks are still judged by the schedule that applied back then,
    /// so changing Monday to Tuesday doesn't turn past Tuesdays into missed days, and doesn't
    /// erase last Monday's miss either.
    /// </remarks>
    public void ChangeDays(IEnumerable<DayOfWeek> newDays, DateOnly today)
    {
        var dayList = CheckDays(newDays);
        _days = dayList;
        ScheduleStartsOn = today;

        var latest = _schedules.OrderByDescending(s => s.StartsOn).FirstOrDefault();
        if (latest != null && latest.StartsOn == today)
        {
            // Changed twice on the same day: the day only has one schedule.
            latest.ReplaceDays(dayList);
        }
        else
        {
            _schedules.Add(new RoutineSchedule(today, dayList));
        }
    }

    /// <summary>Marks <paramref name="date"/> as done.</summary>
    /// <param name="date">The day to tick: today, or a missed day up to 6 days back.</param>
    /// <param name="today">Today's date, passed in so tests can choose it.</param>
    /// <exception cref="ArgumentException">
    /// The day is in the future, is before the routine existed, wasn't one of the routine's days
    /// (under the schedule valid that day), or is more than 6 days ago.
    /// </exception>
    /// <remarks>Ticking a day that's already done does nothing, so clicking twice is safe.</remarks>
    public void Tick(DateOnly date, DateOnly today)
    {
        if (date > today)
        {
            throw new ArgumentException("You can't tick a day that hasn't happened yet.");
        }
        if (!ExistedOn(date))
        {
            throw new ArgumentException($"{Title} didn't exist yet on that day.");
        }
        if (!IsScheduledOn(date))
        {
            throw new ArgumentException($"{Title} isn't scheduled on that day.");
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
    /// Each day is judged by the schedule valid on that day. Days before the routine existed are skipped.
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

    /// <summary>What happened on each of the last <paramref name="days"/> days, oldest first, ending today.</summary>
    /// <param name="today">Today's date.</param>
    /// <param name="days">How many days to look back, including today.</param>
    /// <remarks>Worked out from the schedules and ticks, like <see cref="MissedDays"/>. Nothing is stored.</remarks>
    public List<HistoryDay> History(DateOnly today, int days)
    {
        var history = new List<HistoryDay>();
        for (int i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var status = StatusOn(date, today);
            var canTick = status == DayStatus.Missed && date >= today.AddDays(-LateWindowDays);
            history.Add(new HistoryDay(date, status, canTick));
        }
        return history;
    }

    /// <summary>How many scheduled days in a row were ticked, counting back from today.</summary>
    /// <param name="today">Today's date.</param>
    /// <remarks>
    /// Days off don't break a streak. Today not ticked yet doesn't break it either: the day isn't over.
    /// </remarks>
    public int CurrentStreak(DateOnly today)
    {
        var done = DoneDates();
        int streak = 0;
        for (var date = today; ExistedOn(date); date = date.AddDays(-1))
        {
            if (!IsScheduledOn(date))
            {
                continue;
            }
            if (done.Contains(date))
            {
                streak++;
            }
            else if (date != today)
            {
                break;
            }
        }
        return streak;
    }

    /// <summary>The longest run of scheduled days ticked in a row, ever.</summary>
    /// <param name="today">Today's date.</param>
    public int BestStreak(DateOnly today)
    {
        var done = DoneDates();
        int best = 0;
        int run = 0;
        for (var date = CreatedOn; date <= today; date = date.AddDays(1))
        {
            if (!IsScheduledOn(date))
            {
                continue;
            }
            if (done.Contains(date))
            {
                run++;
                best = Math.Max(best, run);
            }
            else if (date != today)
            {
                run = 0;
            }
        }
        return best;
    }

    private DayStatus StatusOn(DateOnly date, DateOnly today)
    {
        if (!ExistedOn(date))
        {
            return DayStatus.NotStarted;
        }
        if (IsDoneOn(date))
        {
            return DayStatus.Done;
        }
        if (!IsScheduledOn(date))
        {
            return DayStatus.Off;
        }
        return date == today ? DayStatus.Pending : DayStatus.Missed;
    }

    // The ticked dates as a set, so the streak loops can check each day quickly.
    private HashSet<DateOnly> DoneDates() => _completions.Select(c => c.Date).ToHashSet();

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
