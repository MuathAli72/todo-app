using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Proves each user only sees and changes their own tasks and routines.
/// </summary>
/// <remarks>
/// Each test logs in as two different users (A and B), each with their own client and login cookie.
/// These run against a real database, because the per-user filter lives in the database queries.
/// </remarks>
public class OwnershipTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OwnershipTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Tasks_need_a_login()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Please log in first.", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task A_user_cannot_see_another_users_tasks()
    {
        var userA = await NewLoggedInUserAsync();
        var userB = await NewLoggedInUserAsync();
        await CreateTaskAsync(userA, "A's secret task");

        var tasksForB = await userB.GetFromJsonAsync<JsonElement>("/tasks");

        Assert.Equal(0, tasksForB.GetArrayLength());
    }

    [Fact]
    public async Task A_user_cannot_change_or_delete_another_users_task()
    {
        var userA = await NewLoggedInUserAsync();
        var userB = await NewLoggedInUserAsync();
        var taskId = await CreateTaskAsync(userA, "A's task");

        var complete = await userB.PatchAsync($"/tasks/{taskId}/complete", null);
        var delete = await userB.DeleteAsync($"/tasks/{taskId}");

        // To user B, A's task doesn't exist.
        Assert.Equal(HttpStatusCode.NotFound, complete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        // And it's untouched for A.
        var tasksForA = await userA.GetFromJsonAsync<JsonElement>("/tasks");
        var task = Assert.Single(tasksForA.EnumerateArray());
        Assert.False(task.GetProperty("isDone").GetBoolean());
    }

    [Fact]
    public async Task A_user_cannot_see_or_delete_another_users_routine()
    {
        var userA = await NewLoggedInUserAsync();
        var userB = await NewLoggedInUserAsync();
        var created = await userA.PostAsJsonAsync("/routines",
            new { title = "A's gym", days = new[] { "Monday" }, time = (string?)null });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var routineId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var routinesForB = await userB.GetFromJsonAsync<JsonElement>("/routines");
        var delete = await userB.DeleteAsync($"/routines/{routineId}");

        Assert.Equal(0, routinesForB.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var routinesForA = await userA.GetFromJsonAsync<JsonElement>("/routines");
        Assert.Equal(1, routinesForA.GetArrayLength());
    }

    [Fact]
    public async Task The_calendar_link_shows_only_your_routines_without_logging_in()
    {
        var userA = await NewLoggedInUserAsync();
        var userB = await NewLoggedInUserAsync();
        await userA.PostAsJsonAsync("/routines", new { title = "Read", days = new[] { "Monday", "Friday" }, time = "07:30" });
        await userB.PostAsJsonAsync("/routines", new { title = "B's secret", days = new[] { "Monday" }, time = (string?)null });
        var link = await userA.GetFromJsonAsync<JsonElement>("/account/calendar-link");
        var token = link.GetProperty("token").GetString();

        // A calendar app has no login cookie: the secret token in the address is enough.
        var calendarApp = _factory.CreateClient();
        var response = await calendarApp.GetAsync($"/calendar/{token}.ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/calendar", response.Content.Headers.ContentType?.MediaType);
        var ics = await response.Content.ReadAsStringAsync();
        Assert.Contains("SUMMARY:Read", ics);
        Assert.Contains("RRULE:FREQ=WEEKLY;BYDAY=MO,FR", ics);
        Assert.Contains("BEGIN:VALARM", ics);
        Assert.DoesNotContain("B's secret", ics);
    }

    [Fact]
    public async Task Resetting_the_calendar_link_stops_the_old_one()
    {
        var user = await NewLoggedInUserAsync();
        var oldToken = (await user.GetFromJsonAsync<JsonElement>("/account/calendar-link")).GetProperty("token").GetString();

        await user.PostAsync("/account/calendar-link/reset", null);
        var oldLink = await _factory.CreateClient().GetAsync($"/calendar/{oldToken}.ics");

        Assert.Equal(HttpStatusCode.NotFound, oldLink.StatusCode);
    }

    [Fact]
    public async Task History_returns_the_last_30_days()
    {
        var user = await NewLoggedInUserAsync();
        var created = await user.PostAsJsonAsync("/routines",
            new { title = "Stretch", days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }, time = (string?)null });
        var routineId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var history = await user.GetFromJsonAsync<JsonElement>($"/routines/{routineId}/history");

        Assert.Equal(30, history.GetArrayLength());
        // Created today and on every day: today is the only day it existed, and it isn't ticked yet.
        Assert.Equal("pending", history[29].GetProperty("status").GetString());
        Assert.Equal("notstarted", history[0].GetProperty("status").GetString());
    }

    // A new client with its own cookie jar, registered (and so logged in) as a brand-new user.
    private async Task<HttpClient> NewLoggedInUserAsync()
    {
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/account/register", new { email, password = "long-enough-password" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return client;
    }

    private static async Task<string> CreateTaskAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/tasks", new { title });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<JsonElement>();
        return task.GetProperty("id").GetString()!;
    }
}
