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
    private static Routine Gym()
    {
        return new Routine("Gym",
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
            null,
            new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void Ticking_todays_routine()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        gym.Tick(today, today);
        Assert.True(gym.IsDoneOn(today));
    }

    [Fact]
    public void Ticking_a_missed_day_late()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        var monday = new DateOnly(2026, 10, 5);
        gym.Tick(monday, today);
        Assert.True(gym.IsDoneOn(monday));
    }

    [Fact]
    public void Unticking_a_day()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        gym.Tick(today, today);
        gym.Untick(today);
        Assert.False(gym.IsDoneOn(today));
    }

    [Fact]
    public void The_same_tick_arriving_twice()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        gym.Tick(today, today);
        gym.Tick(today, today);
        Assert.True(gym.IsDoneOn(today));
        Assert.Single(gym.Completions);
    }

    [Fact]
    public void Ticking_a_day_the_routine_isnt_on_is_rejected()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        var tuesday = new DateOnly(2026, 10, 6);
        Assert.Throws<ArgumentException>(() => gym.Tick(tuesday, today));
        Assert.Empty(gym.Completions);
    }

    [Fact]
    public void Ticking_a_future_day_is_rejected()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        var friday = new DateOnly(2026, 10, 9);
        Assert.Throws<ArgumentException>(() => gym.Tick(friday, today));
        Assert.Empty(gym.Completions);
    }

    [Fact]
    public void Ticking_a_day_before_the_routine_existed_is_rejected()
    {
        var gym = new Routine("Gym",
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
            null,
            new DateOnly(2026, 10, 7));
        var today = new DateOnly(2026, 10, 7);
        var monday = new DateOnly(2026, 10, 5);
        Assert.Throws<ArgumentException>(() => gym.Tick(monday, today));
        Assert.Empty(gym.Completions);
    }

    [Fact]
    public void A_missed_day_shows_up()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        Assert.Contains(new DateOnly(2026, 10, 5), gym.MissedDays(today));
    }

    [Fact]
    public void Today_is_not_missed_yet()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        Assert.DoesNotContain(today, gym.MissedDays(today));
    }

    [Fact]
    public void Ticking_a_missed_day_late_removes_it_from_missed()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        var monday = new DateOnly(2026, 10, 5);
        gym.Tick(monday, today);
        Assert.DoesNotContain(monday, gym.MissedDays(today));
    }

    [Fact]
    public void A_missed_day_disappears_after_a_week()
    {
        var gym = Gym();
        var nextMonday = new DateOnly(2026, 10, 12);
        Assert.DoesNotContain(new DateOnly(2026, 10, 5), gym.MissedDays(nextMonday));
    }
}