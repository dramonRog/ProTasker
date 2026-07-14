using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskComment;
using ProTasker.Validators.TaskComment;
using Xunit;

namespace ProTasker.UnitTests.Validators.TaskComment
{
    public class UpdateTaskCommentRequestValidatorTests
    {
        private readonly UpdateTaskCommentRequestValidator _validator;

        public UpdateTaskCommentRequestValidatorTests()
        {
            _validator = new UpdateTaskCommentRequestValidator();
        }

        [Fact]
        public void Should_HaveErrorForObject_When_AllFieldsAreNull()
        {
            var request = new UpdateTaskCommentRequest(null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Submit at least one field to update.");
        }

        [Fact]
        public void Should_NotHaveError_When_OnlyTitleIsProvided()
        {
            var request = new UpdateTaskCommentRequest("Updated Title", null);
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x);
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Should_NotHaveError_When_OnlyDescriptionIsProvided()
        {
            var request = new UpdateTaskCommentRequest(null, "Updated Description");
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x);
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_HaveError_When_TitleIsProvidedButEmpty(string invalidTitle)
        {
            var request = new UpdateTaskCommentRequest(invalidTitle, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Title can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_TitleIsTooLong()
        {
            var request = new UpdateTaskCommentRequest(new string('a', 101), null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Title can't contain more than 100 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_HaveError_When_DescriptionIsProvidedButEmpty(string invalidDescription)
        {
            var request = new UpdateTaskCommentRequest(null, invalidDescription);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Comment can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_DescriptionIsTooLong()
        {
            var request = new UpdateTaskCommentRequest(null, new string('a', 2001));
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Comment is too long (max 2000 characters).");
        }
    }
}