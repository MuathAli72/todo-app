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
