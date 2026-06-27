using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class ChangeTaskStatusRequestValidator : AbstractValidator<ChangeTaskStatusRequest>
    {
        public ChangeTaskStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Incorrect task status.");
        }
    }
}

