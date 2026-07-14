using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.User;
using ProTasker.Validators.User;
using Xunit;

namespace ProTasker.UnitTests.Validators.User
{
    public class RevokeTokenRequestValidatorTests
    {
        private readonly RevokeTokenRequestValidator _validator;

        public RevokeTokenRequestValidatorTests()
        {
            _validator = new RevokeTokenRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_TokenIsValid()
        {
            var request = new RevokeTokenRequest("valid_refresh_token_string");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_TokenIsEmpty(string? invalidToken)
        {
            var request = new RevokeTokenRequest(invalidToken!);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
                  .WithErrorMessage("Refresh token is required.");
        }
    }
}