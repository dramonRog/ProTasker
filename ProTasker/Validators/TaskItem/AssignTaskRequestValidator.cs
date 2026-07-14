using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class AssignTaskRequestValidator : AbstractValidator<AssignTaskRequest>
    {
        public AssignTaskRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotNull().WithMessage("Assigned user is required.")
                .NotEqual(Guid.Empty).WithMessage("Assigned user is required.");
        }
    }
}
