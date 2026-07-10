using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class CreateTaskItemRequestValidator : AbstractValidator<CreateTaskItemRequest>
    {
        public CreateTaskItemRequestValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Target project is required.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Task title is required.")
                .MaximumLength(300).WithMessage("The maximum length is 300 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(4000).WithMessage("The maximum length is 4000 characters.")
                .When(x => x.Description is not null);

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("The due date must be in the future.")
                .When(x => x.DueDate.HasValue);

            RuleFor(x => x.BoardId)
                .NotEmpty().WithMessage("Board id cannot be empty.")
                .When(x => x.BoardId.HasValue);
        }
    }
}
