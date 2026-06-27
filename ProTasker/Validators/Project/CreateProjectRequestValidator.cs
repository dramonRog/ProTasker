using FluentValidation;
using ProTasker.DTOs.Requests.Project;

namespace ProTasker.Validators.Project
{
    public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
    {
        public CreateProjectRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required.")
                .MaximumLength(200).WithMessage("The maximum length is 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("The maximum length is 2000 characters.")
                .When(x => x.Description is not null);
        }
    }
}


