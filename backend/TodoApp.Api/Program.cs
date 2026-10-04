using TodoApp.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
var app = builder.Build();



app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());



var list = new TodoList();

app.MapGet("/tasks", () => list.Tasks);

app.MapPost("/tasks", (CreateTaskRequest request) =>
{
    try
    {
        var task = list.AddTask(request.Title);
        return Results.Created($"/tasks/{task.Id}", task);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/tasks/{id}/complete", (Guid id) =>
{
    list.CompleteTask(id);
    return Results.NoContent();
});

app.MapPatch("/tasks/{id}/reopen", (Guid id) =>
{
    list.ReopenTask(id);
    return Results.NoContent();
});

app.MapDelete("/tasks/{id}", (Guid id) =>
{
    list.DeleteTask(id);
    return Results.NoContent();
});

app.MapPatch("/tasks/{id}/priority", (Guid id, ChangePriorityRequest request) =>
{
    if (!Enum.TryParse<Priority>(request.Priority, true, out var priority))
    {
        return Results.BadRequest(new { error = "Priority must be low, medium or high" });
    }

    list.ChangePriority(id, priority);
    return Results.NoContent();
});

app.MapPatch("/tasks/{id}/due-date", (Guid id, ChangeDueDateRequest request) =>
{
    if (!DateTime.TryParse(request.DueDate, out var parsedDate))
    {
        return Results.BadRequest(new { error = "Enter date as YYYY-MM-DD" });
    }

    list.ChangeDueDate(id, parsedDate);
    return Results.NoContent();
});


app.MapPatch("/tasks/{id}/title", (Guid id, EditTitleRequest request) =>
{
    try
    {
        list.EditTitle(id, request.Title);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();
public record CreateTaskRequest(string Title);
public record ChangePriorityRequest(string Priority);
public record ChangeDueDateRequest(string DueDate);
public record EditTitleRequest(string Title);