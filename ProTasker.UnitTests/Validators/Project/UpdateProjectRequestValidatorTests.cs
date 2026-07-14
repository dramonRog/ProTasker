using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.Project;
using ProTasker.Validators.Project;
using Xunit;

namespace ProTasker.UnitTests.Validators.Project
{
    public class UpdateProjectRequestValidatorTests
    {
        private readonly UpdateProjectRequestValidator _validator;

        public UpdateProjectRequestValidatorTests()
        {
            _validator = new UpdateProjectRequestValidator();
        }

        [Fact]
        public void Should_HaveErrorForObject_When_AllFieldsAreNull()
        {
            var request = new UpdateProjectRequest(null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Submit at least one field to update.");
        }

        [Fact]
        public void Should_NotHaveError_When_OnlyNameIsProvided()
        {
            var request = new UpdateProjectRequest("Updated Name", null);
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_NotHaveError_When_OnlyDescriptionIsProvided()
        {
            var request = new UpdateProjectRequest(null, "Updated Description");
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x);
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_HaveError_When_NameIsProvidedButEmpty(string invalidName)
        {
            var request = new UpdateProjectRequest(invalidName, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Project name can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_NameIsTooLong()
        {
            var request = new UpdateProjectRequest(new string('a', 201), null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("The maximum length is 200 characters.");
        }

        [Fact]
        public void Should_HaveError_When_DescriptionIsTooLong()
        {
            var request = new UpdateProjectRequest(null, new string('a', 2001));
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("The maximum length is 2000 characters.");
        }
    }
}