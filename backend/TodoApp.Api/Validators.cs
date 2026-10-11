using FluentValidation;
using System.Data;
using System.Security.Cryptography.X509Certificates;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Please enter a title.");
    }

  
}

public class EditTitleRequestValidator : AbstractValidator<EditTitleRequest>
{
    public EditTitleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Please enter a title.");
    }
}

public class ChangePriorityRequestValidator : AbstractValidator<ChangePriorityRequest>
{
    public ChangePriorityRequestValidator()
    {
        var allowed = new[] { "low", "medium", "high" };

        RuleFor(x => x.Priority)
            .Must(p => p != null && allowed.Contains(p.ToLower()))
            .WithMessage("Priority must be low, medium or high.");
    }
}


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

public static class InputRules
{
    public static bool IsDayName(string? day) =>
        day != null && Enum.GetNames<DayOfWeek>().Contains(day, StringComparer.OrdinalIgnoreCase);

    public static bool IsTimeOrEmpty(string? time) =>
        string.IsNullOrEmpty(time) || TimeOnly.TryParse(time, out _);
}

public class ChangeDaysRequestValidator : AbstractValidator<ChangeDaysRequest>
{
    public ChangeDaysRequestValidator()
    {
        RuleFor(x => x.Days).NotEmpty().WithMessage("Pick at least one day.");
        RuleForEach(x => x.Days).Must(InputRules.IsDayName).WithMessage("That isn't a real day.");
    }
}

public class ChangeTimeRequestValidator : AbstractValidator<ChangeTimeRequest>
{
    public ChangeTimeRequestValidator()
    {
        RuleFor(x => x.Time).Must(InputRules.IsTimeOrEmpty).WithMessage("Please enter a real time.");
    }
}

public class TickRequestValidator : AbstractValidator<TickRequest>
{
    public TickRequestValidator()
    {
        RuleFor(x => x.Date)
            .Must(d => DateOnly.TryParseExact(d, "yyyy-MM-dd", out _))
            .WithMessage("Please enter a date as YYYY-MM-DD.");
    }
}