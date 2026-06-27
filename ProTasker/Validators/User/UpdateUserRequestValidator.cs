using FluentValidation;
using ProTasker.DTOs.Requests.User;

namespace ProTasker.Validators.User
{
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.FirstName is not null || x.LastName is not null || x.Email is not null || x.Password is not null)
                .WithMessage("Submit at least one field to update.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name can't be empty.")
                .MaximumLength(100).WithMessage("The maximum length is 100 characters.")
                .When(x => x.FirstName is not null);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name can't be empty.")
                .MaximumLength(100).WithMessage("The maximum length is 100 characters.")
                .When(x => x.LastName is not null);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email can't be empty.")
                .EmailAddress().WithMessage("Incorrect email format.")
                .MaximumLength(256).WithMessage("The maximum length is 256 characters.")
                .When(x => x.Email is not null);

            RuleFor(x => x.Password)
                .MinimumLength(8).WithMessage("Password must contain at least 8 characters.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.")
                .When(x => x.Password is not null);
        }
    }
}

