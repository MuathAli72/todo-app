using System.Security.Claims;
using System.Security.Cryptography;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TodoApp.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TodoDb")));
builder.Services.AddScoped<TodoListRepository>();
builder.Services.AddScoped<RoutineRepository>();
// Lets TodoDbContext read who is logged in for the current request (for the per-user filter).
builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi();
// A simple "are you alive?" check. Render and the CD pipeline call /health after a deploy.
builder.Services.AddHealthChecks();

// Which websites may call this API. On your laptop: the Vite dev server.
// On the live server: set in Render's settings as Cors__AllowedOrigins__0, so no code change is needed.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

// ---------- Accounts (ASP.NET Core Identity, with a login cookie) ----------
// Identity does the risky parts: password hashing, lockout, and the login cookie.
// Users are stored in the same Postgres database, through TodoDbContext.
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services.AddIdentityCore<IdentityUser>(options =>
    {
        // Length over symbols: at least 8 characters, no forced capitals or symbols.
        // That's current guidance (NIST), and easier for people to remember.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        // 5 wrong passwords in a row locks the account for 5 minutes (Identity's defaults).
    })
    .AddEntityFrameworkStores<TodoDbContext>()
    .AddSignInManager();
builder.Services.ConfigureApplicationCookie(options =>
{
    // Stay logged in for 14 days. Each visit pushes that back (sliding expiry).
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    // An API answers 401/403. Without these, the cookie would try to redirect
    // to a login web page that doesn't exist.
    // They also send the usual { "error": "..." } shape, so the frontend can show the message.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new ErrorResponse("Please log in first."));
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new ErrorResponse("You're not allowed to do that."));
    };
});

var app = builder.Build();

// On Render, the website and the API share one address, and API calls start with /api
// (e.g. /api/tasks). UsePathBase cuts "/api" off, so the endpoints below still match /tasks.
// Requests without /api keep working too: Swagger, /health, and Kubernetes (its front door
// already removes /api).
app.UsePathBase("/api");
// Must come right after UsePathBase, so routing sees the shortened address.
app.UseRouting();

// The website files. They only exist in the Render image (in wwwroot); elsewhere these do nothing.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Todo API"));
}

app.UseCors(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    // Lets the browser send the login cookie to this API from the frontend's address.
    .AllowCredentials());

// Reads the login cookie on every request, then checks who's allowed where.
// The order matters: first "who are you", then "are you allowed".
app.UseAuthentication();
app.UseAuthorization();

// "Today" for the user, not for the server. The browser sends its time zone (like "Asia/Riyadh")
// in the X-Time-Zone header. We take the real time (UTC) and convert it to that zone.
// Missing or unknown zone: use the server's own clock, so Swagger and Postman still work.
static DateOnly Today(string? timeZone)
{
    if (!string.IsNullOrEmpty(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var zone))
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
    }
    return DateOnly.FromDateTime(DateTime.Now);
}

static RoutineResponse ToResponse(Routine r, DateOnly today) => new(
    r.Id,
    r.Title,
    r.Days.Select(d => d.ToString()).ToList(),
    r.Time,
    r.IsScheduledOn(today),
    r.IsDoneOn(today),
    r.MissedDays(today),
    r.CurrentStreak(today),
    r.BestStreak(today));

// The 404 answers, written once so every endpoint says the same thing.
// They carry an { error } message like the 400s, so the frontend can show it.
static IResult TaskNotFound() => Results.NotFound(new ErrorResponse("That task doesn't exist. It may have been deleted."));
static IResult RoutineNotFound() => Results.NotFound(new ErrorResponse("That routine doesn't exist. It may have been deleted."));

app.MapHealthChecks("/health");

// ---------- Account ----------

var accountRoutes = app.MapGroup("/account").WithTags("Account");

accountRoutes.MapPost("/register", async (RegisterRequest request, IValidator<RegisterRequest> validator, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    // A friendlier message than Identity's "Username '...' is already taken".
    if (await users.FindByEmailAsync(request.Email) != null)
    {
        return Results.BadRequest(new ErrorResponse("An account with this email already exists."));
    }

    // The email doubles as the user name, so people log in with their email.
    var user = new IdentityUser { UserName = request.Email, Email = request.Email };
    // Identity checks the password rules, hashes the password and saves the user.
    var created = await users.CreateAsync(user, request.Password);
    if (!created.Succeeded)
    {
        return Results.BadRequest(new ErrorResponse(created.Errors.First().Description));
    }

    // Log the new user in straight away, so they don't have to type it all again.
    await signIn.SignInAsync(user, isPersistent: true);
    return Results.Created("/account/me", new MeResponse(request.Email));
})
.WithSummary("Create an account and log in")
.WithDescription("The password needs at least 8 characters. Sets the login cookie.")
.Produces<MeResponse>(StatusCodes.Status201Created)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

accountRoutes.MapPost("/login", async (LoginRequest request, IValidator<LoginRequest> validator, SignInManager<IdentityUser> signIn) =>
{
    var result = validator.Validate(request);
    if (!result.IsValid)
    {
        return Results.BadRequest(new ErrorResponse(result.Errors[0].ErrorMessage));
    }

    // isPersistent: the cookie survives closing the browser.
    // lockoutOnFailure: wrong passwords count toward the lockout.
    var signedIn = await signIn.PasswordSignInAsync(request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);
    if (signedIn.IsLockedOut)
    {
        return Results.Json(new ErrorResponse("Too many wrong tries. Wait a few minutes."), statusCode: StatusCodes.Status429TooManyRequests);
    }
    if (!signedIn.Succeeded)
    {
        // Same message for a wrong email and a wrong password,
        // so nobody can use this to find out which emails have accounts.
        return Results.Json(new ErrorResponse("Wrong email or password."), statusCode: StatusCodes.Status401Unauthorized);
    }
    return Results.NoContent();
})
.WithSummary("Log in")
.WithDescription("Sets the login cookie. The browser sends it with every request after that.")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
.Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests);

accountRoutes.MapPost("/logout", async (SignInManager<IdentityUser> signIn) =>
{
    await signIn.SignOutAsync();
    return Results.NoContent();
})
.WithSummary("Log out")
.WithDescription("Removes the login cookie. Safe to call even when not logged in.")
.Produces(StatusCodes.Status204NoContent);

accountRoutes.MapGet("/me", (ClaimsPrincipal user) => new MeResponse(user.Identity!.Name!))
.RequireAuthorization()
.WithSummary("Who is logged in")
.WithDescription("The frontend calls this when the page opens. 401 means nobody is logged in.")
.Produces<MeResponse>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

// The user's secret calendar link. Made the first time it's asked for, then the same one every time.
accountRoutes.MapGet("/calendar-link", async (ClaimsPrincipal user, TodoDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var feed = await db.CalendarFeeds.FindAsync(userId);
    if (feed == null)
    {
        feed = new CalendarFeed { UserId = userId, Token = NewCalendarToken() };
        db.CalendarFeeds.Add(feed);
        await db.SaveChangesAsync();
    }
    return new CalendarLinkResponse(feed.Token);
})
.RequireAuthorization()
.WithSummary("Get your secret calendar link")
.WithDescription("Subscribe to /calendar/{token}.ics in a calendar app to get your routines as repeating events with reminders.")
.Produces<CalendarLinkResponse>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

accountRoutes.MapPost("/calendar-link/reset", async (ClaimsPrincipal user, TodoDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var feed = await db.CalendarFeeds.FindAsync(userId);
    if (feed == null)
    {
        feed = new CalendarFeed { UserId = userId };
        db.CalendarFeeds.Add(feed);
    }
    feed.Token = NewCalendarToken();
    await db.SaveChangesAsync();
    return new CalendarLinkResponse(feed.Token);
})
.RequireAuthorization()
.WithSummary("Make a new calendar link")
.WithDescription("The old link stops working. Use this if the link was shared by mistake.")
.Produces<CalendarLinkResponse>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

// The feed itself. No login: calendar apps can't log in, so the secret token in the address is the key.
app.MapGet("/calendar/{token}.ics", async (string token, TodoDbContext db) =>
{
    var feed = await db.CalendarFeeds.FirstOrDefaultAsync(f => f.Token == token);
    if (feed == null)
    {
        return Results.NotFound(new ErrorResponse("This calendar link doesn't work anymore. Get a new one in the app."));
    }
    // Nobody is logged in here, so the per-user filter would hide everything.
    // IgnoreQueryFilters turns it off, and the Where picks this link's owner instead.
    var routines = await db.Routines
        .IgnoreQueryFilters()
        .Where(r => EF.Property<string>(r, "UserId") == feed.UserId)
        .OrderBy(r => r.CreatedOn)
        .ToListAsync();
    return Results.Text(CalendarFeedBuilder.Build(routines, DateTime.UtcNow), "text/calendar; charset=utf-8");
})
.WithTags("Calendar")
.WithSummary("A user's routines as a calendar feed (.ics)")
.Produces<string>(StatusCodes.Status200OK, "text/calendar")
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

// 32 random bytes, written with URL-safe letters: impossible to guess.
static string NewCalendarToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

// ---------- Tasks ----------
// A group gives every endpoint inside it the same address prefix and the same Swagger heading.

// Login required: every task endpoint answers 401 without the login cookie.
var taskRoutes = app.MapGroup("/tasks").WithTags("Tasks").RequireAuthorization();

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

// Login required: every routine endpoint answers 401 without the login cookie.
var routineRoutes = app.MapGroup("/routines").WithTags("Routines").RequireAuthorization();

routineRoutes.MapGet("/", async (RoutineRepository repo, [FromHeader(Name = "X-Time-Zone")] string? timeZone) =>
{
    var today = Today(timeZone);
    var routines = await repo.GetAllAsync();
    return routines.Select(r => ToResponse(r, today));
})
.WithSummary("List all routines, with today's status and missed days");

routineRoutes.MapGet("/{id}/history", async (Guid id, RoutineRepository repo, [FromHeader(Name = "X-Time-Zone")] string? timeZone, int? days) =>
{
    var routine = await repo.GetAsync(id);
    if (routine == null)
    {
        return RoutineNotFound();
    }
    var today = Today(timeZone);
    // 30 days unless asked otherwise, and never more than 90.
    var count = Math.Clamp(days ?? 30, 1, 90);
    var history = routine.History(today, count)
        .Select(d => new HistoryDayResponse(d.Date, d.Status.ToString().ToLowerInvariant(), d.CanTick));
    return Results.Ok(history);
})
.WithSummary("What happened on each of the last days, oldest first")
.WithDescription("Status is notstarted, off, done, missed or pending (today, not ticked yet). canTick is true for missed days that can still be ticked late. ?days= sets how many (default 30, max 90).")
.Produces<List<HistoryDayResponse>>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

routineRoutes.MapPost("/", async (CreateRoutineRequest request, IValidator<CreateRoutineRequest> validator, RoutineRepository repo, [FromHeader(Name = "X-Time-Zone")] string? timeZone) =>
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
        var today = Today(timeZone);
        var routine = new Routine(request.Title, days, time, today);
        repo.Add(routine);
        await repo.SaveAsync();
        return Results.Created($"/routines/{routine.Id}", ToResponse(routine, today));
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

routineRoutes.MapPatch("/{id}/days", async (Guid id, ChangeDaysRequest request, IValidator<ChangeDaysRequest> validator, RoutineRepository repo, [FromHeader(Name = "X-Time-Zone")] string? timeZone) =>
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
        routine.ChangeDays(days, Today(timeZone));
        await repo.SaveAsync();
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
})
.WithSummary("Change a routine's days")
.WithDescription("The new days count from today. Past days keep the schedule they had, so old ticks and misses stay.")
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

routineRoutes.MapPost("/{id}/completions", async (Guid id, TickRequest request, IValidator<TickRequest> validator, RoutineRepository repo, [FromHeader(Name = "X-Time-Zone")] string? timeZone) =>
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
        routine.Tick(date, Today(timeZone));
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

// Any other address that isn't an endpoint or a file gets the website's index.html,
// so the React app loads. (Without the website files, this is just a 404.)
app.MapFallbackToFile("index.html");

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
    List<DateOnly> MissedDays,
    int CurrentStreak,
    int BestStreak);
public record HistoryDayResponse(DateOnly Date, string Status, bool CanTick);
public record CalendarLinkResponse(string Token);
public record ChangeDaysRequest(List<string> Days);
public record ChangeTimeRequest(string? Time);
public record TickRequest(string Date);
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record MeResponse(string Email);

// Every error answer (400, 401, 404, 429) has this shape: { "error": "..." }. Naming it lets Swagger show it.
public record ErrorResponse(string Error);

// Lets the integration tests start this API in memory.
public partial class Program { }
