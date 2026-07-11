using FluentValidation;
using ProTasker.DTOs.Requests.TaskComment;

namespace ProTasker.Validators.TaskComment
{
    public class CreateTaskCommentRequestValidator : AbstractValidator<CreateTaskCommentRequest>
    {
        public CreateTaskCommentRequestValidator() 
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title can't be empty.")
                .MaximumLength(100).WithMessage("Title can't contain more than 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description can't be empty.")
                .MaximumLength(2000).WithMessage("Comment is too long (max 2000 characters).");
        }
    }
}
