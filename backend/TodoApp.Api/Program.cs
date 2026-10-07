using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TodoDb")));
builder.Services.AddScoped<TodoListRepository>();

var app = builder.Build();

app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapGet("/tasks", async (TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    return list.Tasks;
});

app.MapPost("/tasks", async (CreateTaskRequest request, IValidator<CreateTaskRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var list = await repo.LoadAsync();
    try
    {
        var task = list.AddTask(request.Title);
        await repo.SaveAsync(list);
        return Results.Created($"/tasks/{task.Id}", task);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/tasks/{id}/complete", async (Guid id, TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    list.CompleteTask(id);
    await repo.SaveAsync(list);
    return Results.NoContent();
});

app.MapPatch("/tasks/{id}/reopen", async (Guid id, TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    list.ReopenTask(id);
    await repo.SaveAsync(list);
    return Results.NoContent();
});

app.MapPatch("/tasks/{id}/title", async (Guid id, EditTitleRequest request, IValidator<EditTitleRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var list = await repo.LoadAsync();
    try
    {
        list.EditTitle(id, request.Title);
        await repo.SaveAsync(list);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/tasks/{id}/priority", async (Guid id, ChangePriorityRequest request, IValidator<ChangePriorityRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var priority = Enum.Parse<Priority>(request.Priority, true);

    var list = await repo.LoadAsync();
    try
    {
        list.ChangePriority(id, priority);
        await repo.SaveAsync(list);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/tasks/{id}/due-date", async (Guid id, ChangeDueDateRequest request, IValidator<ChangeDueDateRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    DateOnly? dueDate = string.IsNullOrEmpty(request.DueDate) ? null : DateOnly.Parse(request.DueDate);
    TimeOnly? dueTime = string.IsNullOrEmpty(request.DueTime) ? null : TimeOnly.Parse(request.DueTime);

    var list = await repo.LoadAsync();
    try
    {
        list.ChangeDueDate(id, dueDate, dueTime);
        await repo.SaveAsync(list);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/tasks/{id}", async (Guid id, TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    list.DeleteTask(id);
    await repo.SaveAsync(list);
    return Results.NoContent();
});

app.Run();

public record CreateTaskRequest(string Title);
public record EditTitleRequest(string Title);
public record ChangePriorityRequest(string Priority);
public record ChangeDueDateRequest(string? DueDate, string? DueTime);