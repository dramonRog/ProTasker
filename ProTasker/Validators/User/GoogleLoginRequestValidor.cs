using FluentValidation;
using ProTasker.DTOs.Requests.User;

namespace ProTasker.Validators.User
{
    public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
    {
        public GoogleLoginRequestValidator()
        {
            RuleFor(x => x.Credential)
                .NotEmpty().WithMessage("Google credential token is required.");
        }
    }
}