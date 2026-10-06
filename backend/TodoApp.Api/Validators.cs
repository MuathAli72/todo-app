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