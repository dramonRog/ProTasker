using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.Board;
using ProTasker.Validators.Board;
using Xunit;

namespace ProTasker.UnitTests.Validators.Board
{
    public class UpdateBoardRequestValidatorTests
    {
        private readonly UpdateBoardRequestValidator _validator;

        public UpdateBoardRequestValidatorTests()
        {
            _validator = new UpdateBoardRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_AllFieldsAreNull()
        {
            var request = new UpdateBoardRequest(null, null, null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_NotHaveError_When_FieldsAreValid()
        {
            var request = new UpdateBoardRequest("Updated Board", 2, "#000000");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_HaveError_When_NameIsProvidedButEmpty(string invalidName)
        {
            var request = new UpdateBoardRequest(invalidName, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Board name can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_NameIsTooLong()
        {
            var request = new UpdateBoardRequest(new string('a', 101), null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Board name can't contain more than 100 characters.");
        }

        [Fact]
        public void Should_HaveError_When_OrderIndexIsLessThanZero()
        {
            var request = new UpdateBoardRequest(null, -1, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.OrderIndex)
                  .WithErrorMessage("This number can't be less than 0.");
        }

        [Fact]
        public void Should_HaveError_When_ColorIsTooLong()
        {
            var request = new UpdateBoardRequest(null, null, new string('a', 21));
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Color)
                  .WithErrorMessage("Color can't contain more than 20 characters.");
        }
    }
}