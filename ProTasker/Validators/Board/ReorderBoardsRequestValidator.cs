using FluentValidation;
using ProTasker.DTOs.Requests.Board;

namespace ProTasker.Validators.Board
{
    public class ReorderBoardsRequestValidator : AbstractValidator<ReorderBoardsRequest>
    {
        public ReorderBoardsRequestValidator() 
        {
            RuleFor(x => x.BoardIds)
                .NotEmpty().WithMessage("The board IDs list cannot be empty.")
                .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("The board IDs list contains duplicates.");
        }
    }
}
