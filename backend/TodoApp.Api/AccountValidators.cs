using FluentValidation;

/// <summary>Checks <c>POST /account/register</c>: a real-looking email and a password.</summary>
/// <remarks>
/// How long a password must be is set once, in Identity's options in Program.cs.
/// Identity checks it when the account is created, so the rule isn't written twice.
/// </remarks>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Please enter your email.")
            .EmailAddress().WithMessage("Please enter a real email.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Please enter a password.");
    }
}

/// <summary>Checks <c>POST /account/login</c>: both boxes are filled in.</summary>
/// <remarks>Whether they're right is Identity's job. This only stops empty requests.</remarks>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Please enter your email and password.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Please enter your email and password.");
    }
}
