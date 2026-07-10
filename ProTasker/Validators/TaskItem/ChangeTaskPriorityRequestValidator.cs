using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class ChangeTaskPriorityRequestValidator : AbstractValidator<ChangeTaskPriorityRequest>
    {
        public ChangeTaskPriorityRequestValidator()
        {
            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid priority level.");
        }
    }
}
