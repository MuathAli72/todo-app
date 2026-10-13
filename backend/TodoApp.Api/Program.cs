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

// The 404 answers, written once so every endpoint says the same thing.
// They carry an { error } message like the 400s, so the frontend can show it.
static IResult TaskNotFound() => Results.NotFound(new ErrorResponse("That task doesn't exist. It may have been deleted."));
static IResult RoutineNotFound() => Results.NotFound(new ErrorResponse("That routine doesn't exist. It may have been deleted."));

// ---------- Tasks ----------
// A group gives every endpoint inside it the same address prefix and the same Swagger heading.

var taskRoutes = app.MapGroup("/tasks").WithTags("Tasks");

taskRoutes.MapGet("/", async (TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    return list.Tasks;
})
.WithSummary("List all tasks, oldest first");

taskRoutes.MapPost("/", async (CreateTaskRequest request, IValidator<CreateTaskRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
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
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Add a task")
.Produces<TodoItem>(StatusCodes.Status201Created)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

taskRoutes.MapPatch("/{id}/complete", async (Guid id, TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    if (!list.CompleteTask(id))
    {
        return TaskNotFound();
    }
    await repo.SaveAsync(list);
    return Results.NoContent();
})
.WithSummary("Mark a task as done")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

taskRoutes.MapPatch("/{id}/reopen", async (Guid id, TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    if (!list.ReopenTask(id))
    {
        return TaskNotFound();
    }
    await repo.SaveAsync(list);
    return Results.NoContent();
})
.WithSummary("Mark a task as not done")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

taskRoutes.MapPatch("/{id}/title", async (Guid id, EditTitleRequest request, IValidator<EditTitleRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    var list = await repo.LoadAsync();
    try
    {
        if (!list.EditTitle(id, request.Title))
        {
            return TaskNotFound();
        }
        await repo.SaveAsync(list);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Rename a task")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

taskRoutes.MapPatch("/{id}/priority", async (Guid id, ChangePriorityRequest request, IValidator<ChangePriorityRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    var priority = Enum.Parse<Priority>(request.Priority, true);

    var list = await repo.LoadAsync();
    try
    {
        if (!list.ChangePriority(id, priority))
        {
            return TaskNotFound();
        }
        await repo.SaveAsync(list);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Change a task's priority")
.WithDescription("Send \"low\", \"medium\" or \"high\". Numbers are rejected.")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

taskRoutes.MapPatch("/{id}/due-date", async (Guid id, ChangeDueDateRequest request, IValidator<ChangeDueDateRequest> validator, TodoListRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    DateOnly? dueDate = string.IsNullOrEmpty(request.DueDate) ? null : DateOnly.Parse(request.DueDate);
    TimeOnly? dueTime = string.IsNullOrEmpty(request.DueTime) ? null : TimeOnly.Parse(request.DueTime);

    var list = await repo.LoadAsync();
    try
    {
        if (!list.ChangeDueDate(id, dueDate, dueTime))
        {
            return TaskNotFound();
        }
        await repo.SaveAsync(list);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Set or remove a task's due date and time")
.WithDescription("Send empty strings to remove them. A time needs a date.")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

taskRoutes.MapDelete("/{id}", async (Guid id, TodoListRepository repo) =>
{
    var list = await repo.LoadAsync();
    if (!list.DeleteTask(id))
    {
        return TaskNotFound();
    }
    await repo.SaveAsync(list);
    return Results.NoContent();
})
.WithSummary("Delete a task")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

// ---------- Routines ----------

var routineRoutes = app.MapGroup("/routines").WithTags("Routines");

routineRoutes.MapGet("/", async (RoutineRepository repo) =>
{
    var today = Today();
    var routines = await repo.GetAllAsync();
    return routines.Select(r => ToResponse(r, today));
})
.WithSummary("List all routines, with today's status and missed days");

routineRoutes.MapPost("/", async (CreateRoutineRequest request, IValidator<CreateRoutineRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
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
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Add a routine")
.WithDescription("Days are names like \"Monday\". Time is optional, like \"17:00\".")
.Produces<RoutineResponse>(StatusCodes.Status201Created)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

routineRoutes.MapPatch("/{id}/title", async (Guid id, EditTitleRequest request, IValidator<EditTitleRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return RoutineNotFound();
    }

    try
    {
        routine.EditTitle(request.Title);
        await repo.SaveAsync();
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Rename a routine")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

routineRoutes.MapPatch("/{id}/days", async (Guid id, ChangeDaysRequest request, IValidator<ChangeDaysRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return RoutineNotFound();
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
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Change a routine's days")
.WithDescription("Past ticks are kept. Missed-day tracking restarts from today.")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

routineRoutes.MapPatch("/{id}/time", async (Guid id, ChangeTimeRequest request, IValidator<ChangeTimeRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return RoutineNotFound();
    }

    TimeOnly? time = string.IsNullOrEmpty(request.Time) ? null : TimeOnly.Parse(request.Time);
    routine.ChangeTime(time);
    await repo.SaveAsync();
    return Results.NoContent();
})
.WithSummary("Change or remove a routine's time")
.WithDescription("Send an empty string or null to remove the time.")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

routineRoutes.MapDelete("/{id}", async (Guid id, RoutineRepository repo) =>
{
    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return RoutineNotFound();
    }

    repo.Remove(routine);
    await repo.SaveAsync();
    return Results.NoContent();
})
.WithSummary("Delete a routine and all its ticks")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

routineRoutes.MapPost("/{id}/completions", async (Guid id, TickRequest request, IValidator<TickRequest> validator, RoutineRepository repo) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return RoutineNotFound();
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
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
    {
        // Two ticks for the same day arrived at once. The day is ticked either way.
        return Results.NoContent();
    }
})
.WithSummary("Tick a routine for a day")
.WithDescription("Date as YYYY-MM-DD. Only a scheduled day, not in the future, after the routine existed, and at most 6 days back. Ticking the same day twice is harmless.")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

routineRoutes.MapDelete("/{id}/completions/{date}", async (Guid id, DateOnly date, RoutineRepository repo) =>
{
    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return RoutineNotFound();
    }

    routine.Untick(date);
    await repo.SaveAsync();
    return Results.NoContent();
})
.WithSummary("Untick a routine for a day")
.WithDescription("Date as YYYY-MM-DD in the address, like /routines/{id}/completions/2026-10-13.")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

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

// Every error answer (400 and 404) has this shape: { "error": "..." }. Naming it lets Swagger show it.
public record ErrorResponse(string Error);
