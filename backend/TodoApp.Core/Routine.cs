namespace TodoApp.Core;

public class Routine
{
    private List<DayOfWeek> days = new();
    private List<Completion> completions = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; }
    public IReadOnlyList<DayOfWeek> Days => days.AsReadOnly();
    public TimeOnly? Time { get; private set; }
    public DateOnly CreatedOn { get; private set; }
    public IReadOnlyList<Completion> Completions => completions.AsReadOnly();
    public bool ExistedOn(DateOnly date) => date >= CreatedOn;

    public Routine(string title, IEnumerable<DayOfWeek> days, TimeOnly? time, DateOnly createdOn)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Please enter a title.");
        }

        var dayList = days.Distinct().ToList();
        if (dayList.Count == 0)
        {
            throw new ArgumentException("Pick at least one day.");
        }

        Title = title;
        this.days = dayList;
        Time = time;
        CreatedOn = createdOn;
    }

    public bool IsScheduledOn(DateOnly date) => days.Contains(date.DayOfWeek);

    public bool IsDoneOn(DateOnly date) => completions.Any(c => c.Date == date);

    public void Untick(DateOnly date)
    {
        completions.RemoveAll(c => c.Date == date);
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
            throw new ArgumentException($"{Title} didn't exist yet on that day.");
        if (IsDoneOn(date))
        {
            return;
        }

        completions.Add(new Completion(date));
    }

    public List<DateOnly> MissedDays(DateOnly today)
    {
        var missed = new List<DateOnly>();

        for (int i = 1; i <= 6; i++)
        {
            var day = today.AddDays(-i);

            if (!IsScheduledOn(day)) continue;
            if (!ExistedOn(day)) continue;
            if (IsDoneOn(day)) continue;

            missed.Add(day);
        }

        return missed;
    }
}