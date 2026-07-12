using FluentValidation;
using ProTasker.DTOs.Requests.TaskItem;

namespace ProTasker.Validators.TaskItem
{
    public class GetTasksQueryParametersValidator : AbstractValidator<GetTasksQueryParameters>
    {
        public GetTasksQueryParametersValidator() 
        {
            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid priority was specified.")
                .When(x => x.Priority != null);

            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).WithMessage("Search term is too long (max 100 characters).")
                .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
        }
    }
}
