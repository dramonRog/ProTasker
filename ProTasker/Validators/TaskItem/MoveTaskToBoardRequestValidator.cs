using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class MoveTaskToBoardRequestValidator : AbstractValidator<MoveTaskToBoardRequest>
    {
        public MoveTaskToBoardRequestValidator()
        {
            RuleFor(x => x.BoardId)
                .NotEmpty().WithMessage("Target board is required.");
        }
    }
}
