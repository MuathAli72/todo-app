using FluentValidation;
using TodoApp.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
var app = builder.Build();



app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());



var list = new TodoList();

app.MapGet("/tasks", () => list.Tasks);

app.MapPost("/tasks", (CreateTaskRequest request, IValidator<CreateTaskRequest> validator) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

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

app.MapPatch("/tasks/{id}/priority", (Guid id, ChangePriorityRequest request, IValidator<ChangePriorityRequest> validator) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var priority = Enum.Parse<Priority>(request.Priority, true);

    try
    {
        list.ChangePriority(id, priority);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/tasks/{id}/due-date", (Guid id, ChangeDueDateRequest request, IValidator<ChangeDueDateRequest> validator) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    DateOnly? dueDate = string.IsNullOrEmpty(request.DueDate) ? null : DateOnly.Parse(request.DueDate);
    TimeOnly? dueTime = string.IsNullOrEmpty(request.DueTime) ? null : TimeOnly.Parse(request.DueTime);

    try
    {
        list.ChangeDueDate(id, dueDate, dueTime);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/tasks/{id}/title", (Guid id, EditTitleRequest request, IValidator<EditTitleRequest> validator) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }
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
public record ChangeDueDateRequest(string? DueDate, String? DueTime);
public record EditTitleRequest(string Title);