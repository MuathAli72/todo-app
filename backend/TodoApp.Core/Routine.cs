namespace TodoApp.Core;

public class Routine
{
    private List<DayOfWeek> days = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; }
    public IReadOnlyList<DayOfWeek> Days => days.AsReadOnly();
    public TimeOnly? Time { get; private set; }
    public DateOnly CreatedOn { get; private set; }

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
}