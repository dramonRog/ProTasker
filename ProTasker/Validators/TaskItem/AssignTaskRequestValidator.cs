using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class AssignTaskRequestValidator : AbstractValidator<AssignTaskRequest>
    {
        public AssignTaskRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Assigned user is required.");
        }
    }
}
