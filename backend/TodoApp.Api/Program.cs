using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Core;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TodoDb")));
builder.Services.AddScoped<TodoListRepository>();
builder.Services.AddScoped<RoutineRepository>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Todo API"));
}

app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());

static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

static RoutineResponse ToResponse(Routine r, DateOnly today) => new(
    r.Id,
    r.Title,
    r.Days.Select(d => d.ToString()).ToList(),
    r.Time,
    r.IsScheduledOn(today),
    r.IsDoneOn(today),
    r.MissedDays(today));

app.MapGet("/routines", async (RoutineRepository repo) =>
{
    var today = Today();
    var routines = await repo.GetAllAsync();
    return routines.Select(r => ToResponse(r, today));
});

app.MapPost("/routines", async (CreateRoutineRequest request, IValidator<CreateRoutineRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var days = request.Days.Select(d => Enum.Parse<DayOfWeek>(d, true));
    TimeOnly? time = string.IsNullOrEmpty(request.Time) ? null : TimeOnly.Parse(request.Time);

    try
    {
        var routine = new Routine(request.Title, days, time, Today());
        repo.Add(routine);
        await repo.SaveAsync();
        return Results.Created($"/routines/{routine.Id}", ToResponse(routine, Today()));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/routines/{id}/title", async (Guid id, EditTitleRequest request, IValidator<EditTitleRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return Results.NotFound();
    }

    try
    {
        routine.EditTitle(request.Title);
        await repo.SaveAsync();
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/routines/{id}/days", async (Guid id, ChangeDaysRequest request, IValidator<ChangeDaysRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return Results.NotFound();
    }

    try
    {
        var days = request.Days.Select(d => Enum.Parse<DayOfWeek>(d, true));
        routine.ChangeDays(days, Today());
        await repo.SaveAsync();
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPatch("/routines/{id}/time", async (Guid id, ChangeTimeRequest request, IValidator<ChangeTimeRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return Results.NotFound();
    }

    TimeOnly? time = string.IsNullOrEmpty(request.Time) ? null : TimeOnly.Parse(request.Time);
    routine.ChangeTime(time);
    await repo.SaveAsync();
    return Results.NoContent();
});

app.MapDelete("/routines/{id}", async (Guid id, RoutineRepository repo) =>
{
    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return Results.NotFound();
    }

    repo.Remove(routine);
    await repo.SaveAsync();
    return Results.NoContent();
});

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

app.MapPost("/routines/{id}/completions", async (Guid id, TickRequest request, IValidator<TickRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.Errors[0].ErrorMessage });
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return Results.NotFound();
    }

    var date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");

    try
    {
        routine.Tick(date, Today());
        await repo.SaveAsync();
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
    {
        return Results.NoContent();
    }
});

app.MapDelete("/routines/{id}/completions/{date}", async (Guid id, DateOnly date, RoutineRepository repo) =>
{
    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return Results.NotFound();
    }

    routine.Untick(date);
    await repo.SaveAsync();
    return Results.NoContent();
});
app.Run();

public record CreateTaskRequest(string Title);
public record EditTitleRequest(string Title);
public record ChangePriorityRequest(string Priority);
public record ChangeDueDateRequest(string? DueDate, string? DueTime);
public record CreateRoutineRequest(string Title, List<string> Days, string? Time);

public record RoutineResponse(
    Guid Id,
    string Title,
    List<string> Days,
    TimeOnly? Time,
    bool IsToday,
    bool IsDoneToday,
    List<DateOnly> MissedDays);
public record ChangeDaysRequest(List<string> Days);
public record ChangeTimeRequest(string? Time);
public record TickRequest(string Date);