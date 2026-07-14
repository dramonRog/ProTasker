using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskComment;
using ProTasker.Validators.TaskComment;
using Xunit;

namespace ProTasker.UnitTests.Validators.TaskComment
{
    public class CreateTaskCommentRequestValidatorTests
    {
        private readonly CreateTaskCommentRequestValidator _validator;

        public CreateTaskCommentRequestValidatorTests()
        {
            _validator = new CreateTaskCommentRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_ModelIsValid()
        {
            var request = new CreateTaskCommentRequest("Valid Title", "Valid Description");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_TitleIsEmpty(string? invalidTitle)
        {
            var request = new CreateTaskCommentRequest(invalidTitle!, "Valid Description");
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Title can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_TitleIsTooLong()
        {
            var request = new CreateTaskCommentRequest(new string('a', 101), "Valid Description");
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Title can't contain more than 100 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_DescriptionIsEmpty(string? invalidDescription)
        {
            var request = new CreateTaskCommentRequest("Valid Title", invalidDescription!);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_DescriptionIsTooLong()
        {
            var request = new CreateTaskCommentRequest("Valid Title", new string('a', 2001));
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Comment is too long (max 2000 characters).");
        }
    }
}