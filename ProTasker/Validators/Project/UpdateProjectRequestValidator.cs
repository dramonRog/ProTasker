using FluentValidation;
using ProTasker.DTOs.Requests.Project;

namespace ProTasker.Validators.Project
{
    public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
    {
        public UpdateProjectRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Name is not null || x.Description is not null)
                .WithMessage("Submit at least one field to update.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name can't be empty.")
                .MaximumLength(200).WithMessage("The maximum length is 200 characters.")
                .When(x => x.Name is not null);

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("The maximum length is 2000 characters.")
                .When(x => x.Description is not null);
        }
    }
}
