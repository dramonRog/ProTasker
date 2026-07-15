using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using ProTasker.Services.Implementations;
using System.Security.Claims;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class UserContextServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly UserContextService _userContextService;

        public UserContextServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _userContextService = new UserContextService(_httpContextAccessorMock.Object);
        }

        [Fact]
        public void GetCurrentUserId_ShouldReturnGuid_WhenUserIsAuthenticated()
        {
            var expectedUserId = Guid.NewGuid();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            var result = _userContextService.GetCurrentUserId();

            result.Should().Be(expectedUserId);
        }

        [Fact]
        public void GetCurrentUserId_ShouldThrowUnauthorizedAccessException_WhenHttpContextIsNull()
        {
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            Action act = () => _userContextService.GetCurrentUserId();

            act.Should()
               .Throw<UnauthorizedAccessException>()
               .WithMessage("User is not authenticated.");
        }

        [Fact]
        public void GetCurrentUserId_ShouldThrowUnauthorizedAccessException_WhenNameIdentifierClaimIsMissing()
        {
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            Action act = () => _userContextService.GetCurrentUserId();

            act.Should()
               .Throw<UnauthorizedAccessException>()
               .WithMessage("User is not authenticated.");
        }

        [Fact]
        public void GetCurrentUserId_ShouldThrowFormatException_WhenClaimValueIsNotValidGuid()
        {
            var invalidGuidString = "not-a-valid-guid";
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, invalidGuidString) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            Action act = () => _userContextService.GetCurrentUserId();

            act.Should().Throw<FormatException>();
        }
    }
}