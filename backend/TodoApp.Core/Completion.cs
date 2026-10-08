namespace TodoApp.Core;

public class Completion
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateOnly Date { get; private set; }
    public DateTime CompletedAt { get; private set; }

    internal Completion(DateOnly date)
    {
        Date = date;
        CompletedAt = DateTime.UtcNow;
    }
}