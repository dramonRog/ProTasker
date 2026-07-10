using FluentValidation;
using ProTasker.DTOs.Requests.Board;

namespace ProTasker.Validators.Board
{
    public class UpdateBoardRequestValidator : AbstractValidator<UpdateBoardRequest>
    {
        public UpdateBoardRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Board name can't be empty.")
                .MaximumLength(100).WithMessage("Board name can't contain more than 100 characters.")
                .When(x => x.Name != null);

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0).WithMessage("This number can't be less than 0.")
                .When(x => x.OrderIndex.HasValue);

            RuleFor(x => x.Color)
                .MaximumLength(20).WithMessage("Color can't contain more than 20 characters.")
                .When(x => x.Color != null);
        }
    }
}
