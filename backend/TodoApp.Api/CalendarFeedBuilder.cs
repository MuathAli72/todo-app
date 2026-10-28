using System.Text;
using TodoApp.Core;

/// <summary>Turns routines into an iCalendar (.ics) feed that phone and desktop calendars can subscribe to.</summary>
/// <remarks>
/// Each routine becomes one repeating event ("every Mon, Wed, Fri") from the day its current schedule
/// started. Routines with a time get that time plus a reminder alert. Routines without a time
/// become all-day events, with no alert. Times are "floating": 07:00 means 07:00 wherever the phone is.
/// The format is plain text with strict rules (RFC 5545): CRLF line endings, escaped text,
/// and lines folded at 75 characters.
/// </remarks>
public static class CalendarFeedBuilder
{
    /// <summary>Builds the whole .ics text for <paramref name="routines"/>.</summary>
    /// <param name="routines">One user's routines.</param>
    /// <param name="nowUtc">The current time, stamped on each event (required by the format).</param>
    public static string Build(IEnumerable<Routine> routines, DateTime nowUtc)
    {
        var ics = new StringBuilder();
        Line(ics, "BEGIN:VCALENDAR");
        Line(ics, "VERSION:2.0");
        Line(ics, "PRODID:-//todo-app//routines//EN");
        Line(ics, "CALSCALE:GREGORIAN");
        Line(ics, "X-WR-CALNAME:Routines");
        // Asks calendar apps to check for changes every few hours (not all of them listen).
        Line(ics, "X-PUBLISHED-TTL:PT3H");
        Line(ics, "REFRESH-INTERVAL;VALUE=DURATION:PT3H");

        foreach (var routine in routines)
        {
            AddEvent(ics, routine, nowUtc);
        }

        Line(ics, "END:VCALENDAR");
        return ics.ToString();
    }

    private static void AddEvent(StringBuilder ics, Routine routine, DateTime nowUtc)
    {
        // The repeat rule's first day must itself be one of the routine's days.
        var start = routine.ScheduleStartsOn;
        while (!routine.Days.Contains(start.DayOfWeek))
        {
            start = start.AddDays(1);
        }
        // "MO", "WE", "FR"...: the first two letters of each day, in capitals.
        var byDay = string.Join(",", routine.Days.Select(d => d.ToString()[..2].ToUpperInvariant()));

        Line(ics, "BEGIN:VEVENT");
        Line(ics, $"UID:{routine.Id}@todo-app");
        Line(ics, $"DTSTAMP:{nowUtc:yyyyMMdd'T'HHmmss'Z'}");
        Line(ics, $"SUMMARY:{Escape(routine.Title)}");
        if (routine.Time is TimeOnly time)
        {
            Line(ics, $"DTSTART:{start:yyyyMMdd}T{time:HHmmss}");
            Line(ics, "DURATION:PT30M");
        }
        else
        {
            Line(ics, $"DTSTART;VALUE=DATE:{start:yyyyMMdd}");
            Line(ics, $"DTEND;VALUE=DATE:{start.AddDays(1):yyyyMMdd}");
        }
        Line(ics, $"RRULE:FREQ=WEEKLY;BYDAY={byDay}");
        if (routine.Time != null)
        {
            // The reminder: an alert at the routine's time.
            Line(ics, "BEGIN:VALARM");
            Line(ics, "ACTION:DISPLAY");
            Line(ics, $"DESCRIPTION:{Escape(routine.Title)}");
            Line(ics, "TRIGGER:PT0M");
            Line(ics, "END:VALARM");
        }
        Line(ics, "END:VEVENT");
    }

    // Backslash, semicolon, comma and line breaks have special meanings in .ics text.
    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    // Writes one line with CRLF. Lines longer than 75 characters are folded: the rest continues on
    // the next line, which starts with a space.
    private static void Line(StringBuilder ics, string text)
    {
        const int MaxLength = 74;
        while (text.Length > MaxLength)
        {
            ics.Append(text, 0, MaxLength).Append("\r\n ");
            text = text[MaxLength..];
        }
        ics.Append(text).Append("\r\n");
    }
}
