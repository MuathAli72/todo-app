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

    [Fact]
    public void Editing_the_title()
    {
        var gym = Gym();
        gym.EditTitle("Swim");
        Assert.Equal("Swim", gym.Title);
    }

    [Fact]
    public void Editing_the_title_to_empty_is_rejected()
    {
        var gym = Gym();
        Assert.Throws<ArgumentException>(() => gym.EditTitle(""));
        Assert.Equal("Gym", gym.Title);
    }

    [Fact]
    public void Changing_days_keeps_old_completions()
    {
        var gym = Gym();
        var monday = new DateOnly(2026, 10, 5);
        var wednesday = new DateOnly(2026, 10, 7);
        gym.Tick(monday, wednesday);
        gym.ChangeDays(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, wednesday);
        Assert.True(gym.IsDoneOn(monday));
    }

    [Fact]
    public void Changing_days_does_not_make_past_days_missed()
    {
        var gym = Gym();
        var wednesday = new DateOnly(2026, 10, 7);
        gym.ChangeDays(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, wednesday);
        Assert.DoesNotContain(new DateOnly(2026, 10, 6), gym.MissedDays(wednesday));
    }

    [Fact]
    public void Changing_to_no_days_is_rejected()
    {
        var gym = Gym();
        Assert.Throws<ArgumentException>(() => gym.ChangeDays(new DayOfWeek[0], new DateOnly(2026, 10, 7)));
        Assert.Equal(3, gym.Days.Count);
    }

    [Fact]
    public void Ticking_a_day_older_than_the_window_is_rejected()
    {
        // Given gym (Mon/Wed/Fri), and today is Monday Oct 12
        var gym = Gym();
        var today = new DateOnly(2026, 10, 12);



        Assert.Throws<ArgumentException>(() => gym.Tick(new DateOnly(2026, 10, 5), today));
        Assert.Empty(gym.Completions);
    }

    [Fact]
    public void Ticking_a_day_six_days_ago_still_works()
    {

        var gym = Gym();
        var today = new DateOnly(2026, 10, 11);


        gym.Tick(new DateOnly(2026, 10, 5), today);


        Assert.True(gym.IsDoneOn(new DateOnly(2026, 10, 5)));
    }

    // ----- Schedule history -----

    [Fact]
    public void Changing_days_keeps_misses_from_the_old_schedule()
    {
        // Gym was Mon/Wed/Fri. Monday Oct 5 was skipped. On Wednesday the days change to Tue/Thu.
        var gym = Gym();
        var wednesday = new DateOnly(2026, 10, 7);
        gym.ChangeDays(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, wednesday);

        // Monday was a gym day back then, so it's still missed.
        Assert.Contains(new DateOnly(2026, 10, 5), gym.MissedDays(wednesday));
    }

    [Fact]
    public void A_day_from_the_old_schedule_can_still_be_ticked_late()
    {
        var gym = Gym();
        var wednesday = new DateOnly(2026, 10, 7);
        gym.ChangeDays(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, wednesday);

        gym.Tick(new DateOnly(2026, 10, 5), wednesday);

        Assert.True(gym.IsDoneOn(new DateOnly(2026, 10, 5)));
    }

    [Fact]
    public void The_new_days_count_from_the_change_onwards()
    {
        var gym = Gym();
        var wednesday = new DateOnly(2026, 10, 7);
        gym.ChangeDays(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, wednesday);

        Assert.False(gym.IsScheduledOn(new DateOnly(2026, 10, 6))); // Tuesday before the change
        Assert.True(gym.IsScheduledOn(new DateOnly(2026, 10, 8))); // Thursday after the change
        Assert.False(gym.IsScheduledOn(new DateOnly(2026, 10, 9))); // Friday after the change
    }

    [Fact]
    public void Changing_days_twice_on_one_day_keeps_one_schedule_for_that_day()
    {
        var gym = Gym();
        var wednesday = new DateOnly(2026, 10, 7);
        gym.ChangeDays(new[] { DayOfWeek.Tuesday }, wednesday);
        gym.ChangeDays(new[] { DayOfWeek.Thursday }, wednesday);

        Assert.Equal(2, gym.Schedules.Count);
        Assert.Equal(new[] { DayOfWeek.Thursday }, gym.DaysOn(wednesday));
    }

    // ----- History -----

    [Fact]
    public void History_shows_each_day_oldest_first()
    {
        // Gym created Thu Oct 1 (Mon/Wed/Fri). Today is Wed Oct 7. Monday Oct 5 was ticked.
        var gym = Gym();
        var today = new DateOnly(2026, 10, 7);
        gym.Tick(new DateOnly(2026, 10, 5), today);

        var history = gym.History(today, 8);

        Assert.Equal(8, history.Count);
        Assert.Equal(new DateOnly(2026, 9, 30), history[0].Date);
        Assert.Equal(DayStatus.NotStarted, history[0].Status); // Sep 30, before it existed
        Assert.Equal(DayStatus.Missed, history[2].Status); // Fri Oct 2
        Assert.Equal(DayStatus.Off, history[3].Status); // Sat Oct 3
        Assert.Equal(DayStatus.Done, history[5].Status); // Mon Oct 5
        Assert.Equal(DayStatus.Pending, history[7].Status); // today, not ticked yet
    }

    [Fact]
    public void Only_missed_days_inside_the_window_can_be_ticked_from_history()
    {
        var gym = Gym();
        var today = new DateOnly(2026, 10, 12); // Monday

        var history = gym.History(today, 12);

        // Fri Oct 2 is 10 days ago: missed, but too old to tick.
        var friday = history.Single(d => d.Date == new DateOnly(2026, 10, 2));
        Assert.Equal(DayStatus.Missed, friday.Status);
        Assert.False(friday.CanTick);
        // Fri Oct 9 is 3 days ago: can still be ticked.
        Assert.True(history.Single(d => d.Date == new DateOnly(2026, 10, 9)).CanTick);
    }

    // ----- Streaks -----

    [Fact]
    public void Days_off_do_not_break_a_streak()
    {
        // Mon/Wed/Fri. Mon 5, Wed 7 and Fri 9 were ticked. Today is Sat Oct 10.
        var gym = Gym();
        var today = new DateOnly(2026, 10, 10);
        foreach (var day in new[] { 5, 7, 9 })
        {
            gym.Tick(new DateOnly(2026, 10, day), today);
        }

        Assert.Equal(3, gym.CurrentStreak(today));
    }

    [Fact]
    public void Today_not_ticked_yet_does_not_break_the_streak()
    {
        var gym = Gym();
        var monday = new DateOnly(2026, 10, 12);
        gym.Tick(new DateOnly(2026, 10, 9), monday);

        Assert.Equal(1, gym.CurrentStreak(monday));
    }

    [Fact]
    public void A_missed_day_ends_the_streak_but_best_streak_remembers()
    {
        // Ticked Fri Oct 2 and Mon Oct 5, missed Wed Oct 7, ticked Fri Oct 9. Today is Fri Oct 9.
        var today = new DateOnly(2026, 10, 9);
        var early = new Routine("Gym", new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }, null, new DateOnly(2026, 10, 1));
        early.Tick(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 2));
        early.Tick(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 5));
        early.Tick(new DateOnly(2026, 10, 9), today);

        Assert.Equal(1, early.CurrentStreak(today));
        Assert.Equal(2, early.BestStreak(today));
    }
}
