using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.User;
using ProTasker.Validators.User;
using Xunit;

namespace ProTasker.UnitTests.Validators.User
{
    public class LoginUserRequestValidatorTests
    {
        private readonly LoginUserRequestValidator _validator;

        public LoginUserRequestValidatorTests()
        {
            _validator = new LoginUserRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_ModelIsValid()
        {
            var request = new LoginUserRequest("test@example.com", "MySecretPassword123!");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_EmailIsEmpty(string? invalidEmail)
        {
            var request = new LoginUserRequest(invalidEmail!, "Password123!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email is required.");
        }

        [Fact]
        public void Should_HaveError_When_EmailIsInvalid()
        {
            var request = new LoginUserRequest("invalid-email-format", "Password123!");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Incorrect email format.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_PasswordIsEmpty(string? invalidPassword)
        {
            var request = new LoginUserRequest("test@example.com", invalidPassword!);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password is required.");
        }
    }
}