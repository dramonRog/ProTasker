using FluentValidation;
using ProTasker.DTOs.Requests.ProjectMember;

namespace ProTasker.Validators.ProjectMember
{
    public class ChangeProjectMemberRoleRequestValidator : AbstractValidator<ChangeProjectMemberRole>
    {
        public ChangeProjectMemberRoleRequestValidator()
        {
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid role specified.");
        }
    }
}