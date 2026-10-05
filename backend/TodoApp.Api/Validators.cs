using FluentValidation;

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