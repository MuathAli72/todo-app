namespace TodoApp.Core;

public class Routine
{
    private const int LateWindowDays = 6;

    private List<DayOfWeek> days = new();
    private List<Completion> completions = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; }
    public IReadOnlyList<DayOfWeek> Days => days.AsReadOnly();
    public TimeOnly? Time { get; private set; }
    public DateOnly CreatedOn { get; private set; }
    public DateOnly ScheduleStartsOn { get; private set; }
    public IReadOnlyList<Completion> Completions => completions.AsReadOnly();

    private Routine()
    {
        Title = "";
    }

    public Routine(string title, IEnumerable<DayOfWeek> days, TimeOnly? time, DateOnly createdOn)
    {
        CheckTitle(title);
        var dayList = CheckDays(days);
        Title = title;
        this.days = dayList;
        Time = time;
        CreatedOn = createdOn;
        ScheduleStartsOn = createdOn;
    }

    public bool IsScheduledOn(DateOnly date) => days.Contains(date.DayOfWeek);
    public bool ExistedOn(DateOnly date) => date >= ScheduleStartsOn;
    public bool IsDoneOn(DateOnly date) => completions.Any(c => c.Date == date);

    public void EditTitle(string newTitle)
    {
        CheckTitle(newTitle);
        Title = newTitle;
    }

    public void ChangeTime(TimeOnly? newTime)
    {
        Time = newTime;
    }

    public void ChangeDays(IEnumerable<DayOfWeek> newDays, DateOnly today)
    {
        days = CheckDays(newDays);
        ScheduleStartsOn = today;
    }

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
        if (IsDoneOn(date))
        {
            return;
        }
        completions.Add(new Completion(date));
    }

    public void Untick(DateOnly date)
    {
        completions.RemoveAll(c => c.Date == date);
    }

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

    private static void CheckTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Please enter a title.");
        }
    }

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