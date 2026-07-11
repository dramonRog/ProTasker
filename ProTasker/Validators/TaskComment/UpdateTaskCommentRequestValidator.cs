using FluentValidation;
using ProTasker.DTOs.Requests.TaskComment;

namespace ProTasker.Validators.TaskComment
{
    public class UpdateTaskCommentRequestValidator : AbstractValidator<UpdateTaskCommentRequest>
    {
        public UpdateTaskCommentRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Title != null || x.Description != null)
                .WithMessage("Submit at least one field to update.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title can't be empty.")
                .MaximumLength(100).WithMessage("Title can't contain more than 100 characters.")
                .When(x => x.Title != null);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Comment can't be empty.")
                .MaximumLength(2000).WithMessage("Comment is too long (max 2000 characters).")
                .When(x => x.Description != null);
        }
    }
}
