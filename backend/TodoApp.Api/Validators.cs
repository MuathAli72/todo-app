using FluentValidation;
using System.Data;
using System.Security.Cryptography.X509Certificates;

// FluentValidation checks the shape of each request before the domain sees it,
// so bad input gets a clear 400 message. The domain (TodoApp.Core) still has its own rules:
// these checks are a first filter, the domain rules are the real authority.

/// <summary>Checks <c>POST /tasks</c>: the title can't be empty or only spaces.</summary>
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Please enter a title.");
    }

  
}

/// <summary>Checks a rename (tasks and routines): the title can't be empty or only spaces.</summary>
public class EditTitleRequestValidator : AbstractValidator<EditTitleRequest>
{
    public EditTitleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Please enter a title.");
    }
}

/// <summary>Checks <c>PATCH /tasks/{id}/priority</c>: it must be "low", "medium" or "high", in any capitals.</summary>
public class ChangePriorityRequestValidator : AbstractValidator<ChangePriorityRequest>
{
    public ChangePriorityRequestValidator()
    {
        var allowed = new[] { "low", "medium", "high" };

        // ToLower, so "High" and "HIGH" work too. The null check stops a crash when priority is missing.
        RuleFor(x => x.Priority)
            .Must(p => p != null && allowed.Contains(p.ToLower()))
            .WithMessage("Priority must be low, medium or high.");
    }
}


/// <summary>Checks <c>PATCH /tasks/{id}/due-date</c>: each value is empty (meaning remove) or a real date or time.</summary>
/// <remarks>The rule "a time needs a date" lives in TodoItem, not here.</remarks>
public class ChangeDueDateRequestValidator : AbstractValidator<ChangeDueDateRequest>
{
    public ChangeDueDateRequestValidator()
    {
        RuleFor(x => x.DueDate)
            .Must(d => string.IsNullOrEmpty(d) || DateOnly.TryParse(d, out _))
            .WithMessage("Please enter a real date.");
        RuleFor(x => x.DueTime)
            .Must(d => string.IsNullOrEmpty(d) || TimeOnly.TryParse(d, out _))
            .WithMessage("Please enter a real time.");
    }
}

/// <summary>Checks <c>POST /routines</c>: a title, at least one real day name, and an optional real time.</summary>
public class CreateRoutineRequestValidator : AbstractValidator<CreateRoutineRequest>
{
    public CreateRoutineRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please enter a title.");
        RuleFor(x => x.Days).NotEmpty().WithMessage("Pick at least one day.");
        RuleForEach(x => x.Days).Must(InputRules.IsDayName).WithMessage("That isn't a real day.");
        RuleFor(x => x.Time).Must(InputRules.IsTimeOrEmpty).WithMessage("Please enter a real time.");
    }
}

/// <summary>Small checks shared by the routine validators, so each rule is written once.</summary>
public static class InputRules
{
    /// <summary>True for an English day name like "Monday", in any capitals.</summary>
    public static bool IsDayName(string? day) =>
        day != null && Enum.GetNames<DayOfWeek>().Contains(day, StringComparer.OrdinalIgnoreCase);

    /// <summary>True for no time (null or empty) or a real time like "07:30".</summary>
    public static bool IsTimeOrEmpty(string? time) =>
        string.IsNullOrEmpty(time) || TimeOnly.TryParse(time, out _);
}

/// <summary>Checks <c>PATCH /routines/{id}/days</c>: at least one day, all real day names.</summary>
public class ChangeDaysRequestValidator : AbstractValidator<ChangeDaysRequest>
{
    public ChangeDaysRequestValidator()
    {
        RuleFor(x => x.Days).NotEmpty().WithMessage("Pick at least one day.");
        RuleForEach(x => x.Days).Must(InputRules.IsDayName).WithMessage("That isn't a real day.");
    }
}

/// <summary>Checks <c>PATCH /routines/{id}/time</c>: empty (meaning remove the time) or a real time.</summary>
public class ChangeTimeRequestValidator : AbstractValidator<ChangeTimeRequest>
{
    public ChangeTimeRequestValidator()
    {
        RuleFor(x => x.Time).Must(InputRules.IsTimeOrEmpty).WithMessage("Please enter a real time.");
    }
}

/// <summary>Checks <c>POST /routines/{id}/completions</c>: the date must be exactly yyyy-MM-dd.</summary>
/// <remarks>
/// TryParseExact, so a date like 10/12/2026 can't be read as the wrong day.
/// Which days can be ticked is decided by <see cref="TodoApp.Core.Routine.Tick"/>.
/// </remarks>
public class TickRequestValidator : AbstractValidator<TickRequest>
{
    public TickRequestValidator()
    {
        RuleFor(x => x.Date)
            .Must(d => DateOnly.TryParseExact(d, "yyyy-MM-dd", out _))
            .WithMessage("Please enter a date as YYYY-MM-DD.");
    }
}