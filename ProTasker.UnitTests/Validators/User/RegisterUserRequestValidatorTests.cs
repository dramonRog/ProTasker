using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.User;
using ProTasker.Validators.User;
using Xunit;

namespace ProTasker.UnitTests.Validators.User
{
    public class RegisterUserRequestValidatorTests
    {
        private readonly RegisterUserRequestValidator _validator;

        public RegisterUserRequestValidatorTests()
        {
            _validator = new RegisterUserRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_ModelIsValid()
        {
            var request = new RegisterUserRequest("John", "Doe", "john@example.com", "StrongP@ssw0rd!");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_HaveError_When_FirstNameIsTooLong()
        {
            var request = new RegisterUserRequest(new string('a', 101), "Doe", "test@test.com", "ValidPass1!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.FirstName)
                  .WithErrorMessage("The maximum length is 100 characters.");
        }

        [Fact]
        public void Should_HaveError_When_EmailIsTooLong()
        {
            var longEmail = new string('a', 250) + "@test.com"; 
            var request = new RegisterUserRequest("John", "Doe", longEmail, "ValidPass1!");
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
        public void Should_HaveError_When_PasswordDoesNotMeetRequirements(string invalidPassword, string expectedMessage)
        {
            var request = new RegisterUserRequest("John", "Doe", "test@test.com", invalidPassword);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage(expectedMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_FirstNameIsEmpty(string? invalidFirstName)
        {
            var request = new RegisterUserRequest(invalidFirstName!, "Doe", "test@test.com", "ValidPass1!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.FirstName)
                  .WithErrorMessage("First name is required.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_LastNameIsEmpty(string? invalidLastName)
        {
            var request = new RegisterUserRequest("John", invalidLastName!, "test@test.com", "ValidPass1!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.LastName)
                  .WithErrorMessage("Last name is required.");
        }

        [Fact]
        public void Should_HaveError_When_LastNameIsTooLong()
        {
            var request = new RegisterUserRequest("John", new string('a', 101), "test@test.com", "ValidPass1!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.LastName)
                  .WithErrorMessage("The maximum length is 100 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_EmailIsEmpty(string? invalidEmail)
        {
            var request = new RegisterUserRequest("John", "Doe", invalidEmail!, "ValidPass1!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email is required.");
        }

        [Fact]
        public void Should_HaveError_When_EmailIsInvalid()
        {
            var request = new RegisterUserRequest("John", "Doe", "invalid-email", "ValidPass1!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Invalid email format.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_PasswordIsEmpty(string? invalidPassword)
        {
            var request = new RegisterUserRequest("John", "Doe", "test@test.com", invalidPassword!);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password is required.");
        }
    }
}