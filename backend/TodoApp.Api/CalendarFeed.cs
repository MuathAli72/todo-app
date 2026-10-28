/// <summary>A user's secret calendar link: GET /calendar/{Token}.ics returns their routines.</summary>
/// <remarks>
/// Calendar apps can't log in, so the link itself is the key: a long random token nobody can guess.
/// Anyone with the link can read that user's routine names and times, so it can be reset
/// (POST /account/calendar-link/reset), which makes the old link stop working.
/// </remarks>
public class CalendarFeed
{
    /// <summary>The owner (one link per user).</summary>
    public string UserId { get; set; } = "";
    /// <summary>The secret part of the link.</summary>
    public string Token { get; set; } = "";
}
