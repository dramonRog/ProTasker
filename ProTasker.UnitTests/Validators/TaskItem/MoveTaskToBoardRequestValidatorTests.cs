using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.Validators.TaskItem;
using Xunit;

namespace ProTasker.UnitTests.Validators.TaskItem
{
    public class MoveTaskToBoardRequestValidatorTests
    {
        private readonly MoveTaskToBoardRequestValidator _validator;

        public MoveTaskToBoardRequestValidatorTests()
        {
            _validator = new MoveTaskToBoardRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_BoardIdIsValid()
        {
            var request = new MoveTaskToBoardRequest(Guid.NewGuid());
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.BoardId);
        }

        [Fact]
        public void Should_HaveError_When_BoardIdIsEmpty()
        {
            var request = new MoveTaskToBoardRequest(Guid.Empty);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.BoardId)
                  .WithErrorMessage("Target board is required.");
        }
    }
}