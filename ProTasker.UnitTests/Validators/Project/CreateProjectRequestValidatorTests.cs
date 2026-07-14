using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.Project;
using ProTasker.Validators.Project;
using Xunit;

namespace ProTasker.UnitTests.Validators.Project
{
    public class CreateProjectRequestValidatorTests
    {
        private readonly CreateProjectRequestValidator _validator;

        public CreateProjectRequestValidatorTests()
        {
            _validator = new CreateProjectRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_ModelIsValid()
        {
            var request = new CreateProjectRequest("Valid Project Name", "Valid Description");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_NotHaveError_When_DescriptionIsNull()
        {
            var request = new CreateProjectRequest("Valid Project Name", null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_NameIsEmpty(string? invalidName)
        {
            var request = new CreateProjectRequest(invalidName!, "Valid Description");
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Project name is required.");
        }

        [Fact]
        public void Should_HaveError_When_NameIsTooLong()
        {
            var request = new CreateProjectRequest(new string('a', 201), "Valid Description");
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("The maximum length is 200 characters.");
        }

        [Fact]
        public void Should_HaveError_When_DescriptionIsTooLong()
        {
            var request = new CreateProjectRequest("Valid Project Name", new string('a', 2001));
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("The maximum length is 2000 characters.");
        }
    }
}