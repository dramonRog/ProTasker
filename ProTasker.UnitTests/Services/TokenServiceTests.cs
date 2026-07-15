using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ProTasker.Models;
using ProTasker.Services.Implementations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class TokenServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly TokenService _tokenService;

        public TokenServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();

            _configurationMock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretKeyThatIsAtLeast32BytesLong!");
            _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _configurationMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("60");

            _tokenService = new TokenService(_configurationMock.Object);
        }

        [Fact]
        public void CreateToken_ShouldReturnValidJwtToken_WithCorrectClaimsAndExpiration()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "testuser@example.com"
            };

            var result = _tokenService.CreateToken(user);

            result.Token.Should().NotBeNullOrWhiteSpace();

            result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(result.Token);

            jwtToken.Issuer.Should().Be("TestIssuer");
            jwtToken.Audiences.First().Should().Be("TestAudience");

            var nameIdentifierClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            nameIdentifierClaim.Should().NotBeNull();
            nameIdentifierClaim!.Value.Should().Be(user.Id.ToString());

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            emailClaim.Should().NotBeNull();
            emailClaim!.Value.Should().Be(user.Email);
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnValidBase64String()
        {
            var refreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.Should().NotBeNullOrWhiteSpace();

            Action act = () => Convert.FromBase64String(refreshToken);
            act.Should().NotThrow();

            refreshToken.Length.Should().Be(48);
        }

        [Fact]
        public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
        {
            var token1 = _tokenService.GenerateRefreshToken();
            var token2 = _tokenService.GenerateRefreshToken();

            token1.Should().NotBe(token2);
        }
    }
}