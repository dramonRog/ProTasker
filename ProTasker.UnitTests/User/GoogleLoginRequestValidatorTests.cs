using FluentValidation.TestHelper; 
using ProTasker.DTOs.Requests.User;
using ProTasker.Validators.User;
using Xunit;

namespace ProTasker.UnitTests.Validators.User
{
    public class GoogleLoginRequestValidatorTests
    {
        private readonly GoogleLoginRequestValidator _validator;

        public GoogleLoginRequestValidatorTests()
        {
            _validator = new GoogleLoginRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_CredentialIsValid()
        {
            var request = new GoogleLoginRequest("valid_google_jwt_token_here");

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Credential);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_CredentialIsNullOrEmpty(string? invalidCredential)
        {
            var request = new GoogleLoginRequest(invalidCredential!);

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Credential)
                  .WithErrorMessage("Google credential token is required.");
        }
    }
}