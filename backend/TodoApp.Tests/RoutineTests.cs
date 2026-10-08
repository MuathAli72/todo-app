using TodoApp.Core;
using Xunit;

public class RoutineTests
{
    [Fact]
    public void Creating_a_routine()
    {
        var routine = new Routine("Gym",
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
            null,
            new DateOnly(2026, 10, 1));

        Assert.Equal("Gym", routine.Title);
        Assert.Equal(3, routine.Days.Count);
        Assert.Contains(DayOfWeek.Wednesday, routine.Days);
    }

    [Fact]
    public void Creating_a_routine_with_no_days_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new Routine("Gym", new DayOfWeek[0], null, new DateOnly(2026, 10, 1)));
    }

    [Fact]
    public void Creating_a_routine_with_an_empty_title_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new Routine("", new[] { DayOfWeek.Monday }, null, new DateOnly(2026, 10, 1)));
    }
}