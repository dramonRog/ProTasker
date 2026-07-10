using FluentValidation;
using ProTasker.DTOs.Requests.Board;

namespace ProTasker.Validators.Board
{
    public class CreateBoardRequestValidator : AbstractValidator<CreateBoardRequest>
    {
        public CreateBoardRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Board name is required.")
                .MaximumLength(100).WithMessage("The board name can't contain more than 100 characters.");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0).WithMessage("This number can't be less than 0.");

            RuleFor(x => x.Color)
                .MaximumLength(20).WithMessage("Color can't contain more than 20 characters.")
                .When(x => x.Color != null);
        }
    }
}
