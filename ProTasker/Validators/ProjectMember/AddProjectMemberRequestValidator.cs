using FluentValidation;
using ProTasker.DTOs.Requests.ProjectMember;

namespace ProTasker.Validators.ProjectMember
{
    public class AddProjectMemberRequestValidator : AbstractValidator<AddProjectMemberRequest>
    {
        public AddProjectMemberRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Select a user to add to this project.");
        }
    }

}
