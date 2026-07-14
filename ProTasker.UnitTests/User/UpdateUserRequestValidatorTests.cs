using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.User;
using ProTasker.Validators.User;
using Xunit;

namespace ProTasker.UnitTests.Validators.User
{
    public class UpdateUserRequestValidatorTests
    {
        private readonly UpdateUserRequestValidator _validator;

        public UpdateUserRequestValidatorTests()
        {
            _validator = new UpdateUserRequestValidator();
        }

        [Fact]
        public void Should_HaveErrorForObject_When_AllFieldsAreNull()
        {
            var request = new UpdateUserRequest(null, null, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Submit at least one field to update.");
        }

        [Fact]
        public void Should_NotHaveError_When_AtLeastOneFieldIsProvided()
        {
            var request = new UpdateUserRequest("NewName", null, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x);
            result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
        }

        [Fact]
        public void Should_HaveError_When_FirstNameIsProvidedButEmpty()
        {
            var request = new UpdateUserRequest("", null, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.FirstName)
                  .WithErrorMessage("First name can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_EmailIsProvidedButInvalid()
        {
            var request = new UpdateUserRequest(null, null, "invalid-email", null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Incorrect email format.");
        }

        [Fact]
        public void Should_HaveError_When_FirstNameIsTooLong()
        {
            var request = new UpdateUserRequest(new string('a', 101), null, null, null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.FirstName)
                  .WithErrorMessage("The maximum length is 100 characters.");
        }

        [Fact]
        public void Should_HaveError_When_LastNameIsProvidedButEmpty()
        {
            var request = new UpdateUserRequest(null, "", null, null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.LastName)
                  .WithErrorMessage("Last name can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_LastNameIsTooLong()
        {
            var request = new UpdateUserRequest(null, new string('a', 101), null, null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.LastName)
                  .WithErrorMessage("The maximum length is 100 characters.");
        }

        [Fact]
        public void Should_HaveError_When_EmailIsProvidedButEmpty()
        {
            var request = new UpdateUserRequest(null, null, "", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_EmailIsTooLong()
        {
            var request = new UpdateUserRequest(null, null, new string('a', 250) + "@test.com", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("The maximum length is 256 characters.");
        }

        [Theory]
        [InlineData("short1!", "Password must contain at least 8 characters.")]
        [InlineData("NOLOWERCASE1!", "Password must contain at least one lowercase letter.")]
        [InlineData("nouppercase1!", "Password must contain at least one uppercase letter.")]
        [InlineData("NoDigitPass!", "Password must contain at least one number.")]
        [InlineData("NoSpecialChar1", "Password must contain at least one special character.")]
        public void Should_HaveError_When_PasswordIsProvidedButDoesNotMeetRequirements(string invalidPassword, string expectedMessage)
        {
            var request = new UpdateUserRequest(null, null, null, invalidPassword);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage(expectedMessage);
        }
    }
}