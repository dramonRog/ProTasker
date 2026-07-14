using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.User;
using ProTasker.Validators.User;
using Xunit;

namespace ProTasker.UnitTests.Validators.User
{
    public class RefreshTokenRequestValidatorTests
    {
        private readonly RefreshTokenRequestValidator _validator;

        public RefreshTokenRequestValidatorTests()
        {
            _validator = new RefreshTokenRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_TokenIsValid()
        {
            var request = new RefreshTokenRequest("valid_refresh_token_string");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_TokenIsEmpty(string? invalidToken)
        {
            var request = new RefreshTokenRequest(invalidToken!);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
                  .WithErrorMessage("Refresh token is required.");
        }
    }
}