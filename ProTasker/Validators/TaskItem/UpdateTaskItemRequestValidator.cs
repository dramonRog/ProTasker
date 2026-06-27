using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class UpdateTaskItemRequestValidator : AbstractValidator<UpdateTaskItemRequest>
    {
        public UpdateTaskItemRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Title is not null || x.Description is not null || x.DueDate.HasValue)
                .WithMessage("Submit at least one field to update.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Task title can't be empty.")
                .MaximumLength(300).WithMessage("The maximum length is 300 characters.")
                .When(x => x.Title is not null);

            RuleFor(x => x.Description)
                .MaximumLength(4000).WithMessage("The maximum length is 4000 characters.")
                .When(x => x.Description is not null);

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
                .When(x => x.DueDate.HasValue);
        }
    }

}

