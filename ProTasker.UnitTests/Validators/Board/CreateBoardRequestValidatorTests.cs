using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.Board;
using ProTasker.Validators.Board;
using Xunit;

namespace ProTasker.UnitTests.Validators.Board
{
    public class CreateBoardRequestValidatorTests
    {
        private readonly CreateBoardRequestValidator _validator;

        public CreateBoardRequestValidatorTests()
        {
            _validator = new CreateBoardRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_ModelIsValid()
        {
            var request = new CreateBoardRequest("Valid Board", 1, "#FFFFFF");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_NotHaveError_When_ColorIsNull()
        {
            var request = new CreateBoardRequest("Valid Board", 0, null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_NameIsEmpty(string? invalidName)
        {
            var request = new CreateBoardRequest(invalidName!, 1, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Board name is required.");
        }

        [Fact]
        public void Should_HaveError_When_NameIsTooLong()
        {
            var request = new CreateBoardRequest(new string('a', 101), 1, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("The board name can't contain more than 100 characters.");
        }

        [Fact]
        public void Should_HaveError_When_OrderIndexIsLessThanZero()
        {
            var request = new CreateBoardRequest("Board Name", -1, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.OrderIndex)
                  .WithErrorMessage("This number can't be less than 0.");
        }

        [Fact]
        public void Should_HaveError_When_ColorIsTooLong()
        {
            var request = new CreateBoardRequest("Board Name", 1, new string('a', 21));
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Color)
                  .WithErrorMessage("Color can't contain more than 20 characters.");
        }
    }
}