using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Common;
using ProTasker.Models;
using ProTasker.Data;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Models.Entities;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _mapperMock = new Mock<IMapper>();
            _tokenServiceMock = new Mock<ITokenService>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _configurationMock = new Mock<IConfiguration>();

            _configurationMock.Setup(c => c["Authentication:Google:ClientId"]).Returns("test-client-id");

            _authService = new AuthService(
                _context,
                _mapperMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object,
                _configurationMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }


        [Fact]
        public async Task RegisterAsync_ShouldReturnSuccess_WhenDataIsValid()
        {
            var request = new RegisterUserRequest("John", "Doe", "test@test.com", "Password123!");
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<User>()))
                .Returns(("jwt_token", DateTime.UtcNow.AddHours(1)));
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");
            _mapperMock.Setup(x => x.Map<UserResponse>(It.IsAny<User>()))
                .Returns(new UserResponse(Guid.NewGuid(), "John", "Doe", "test@test.com", DateTime.UtcNow));

            var result = await _authService.RegisterAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Token.Should().Be("jwt_token");
            _context.Users.Count().Should().Be(1);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnConflict_WhenEmailExists()
        {
            var existingUser = new User { Email = "test@test.com", FirstName = "A", LastName = "B", PasswordHash = "hash" };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = new RegisterUserRequest("John", "Doe", "TEST@test.com", "Password123!");

            var result = await _authService.RegisterAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Email is already in use.");
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnConflict_OnDbUpdateException_RaceCondition()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var faultyContext = new FaultyDbContext(options);
            var serviceWithFaultyDb = new AuthService(faultyContext, _mapperMock.Object, _tokenServiceMock.Object, _loggerMock.Object, _configurationMock.Object);

            var request = new RegisterUserRequest("John", "Doe", "test@test.com", "Password123!");

            var result = await serviceWithFaultyDb.RegisterAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Email is already in use.");
        }


        [Fact]
        public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
        {
            var password = "Password123!";
            var user = new User { Email = "test@test.com", FirstName = "John", LastName = "Doe", PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<User>()))
                .Returns(("jwt_token", DateTime.UtcNow.AddHours(1)));
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            var request = new LoginUserRequest("TEST@test.com", password);

            var result = await _authService.LoginAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Token.Should().Be("jwt_token");
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnUnauthorized_WhenUserDoesNotExist()
        {
            var request = new LoginUserRequest("nonexistent@test.com", "Password123!");

            var result = await _authService.LoginAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid email or password.");
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnUnauthorized_WhenPasswordIsIncorrect()
        {
            var user = new User { Email = "test@test.com", FirstName = "John", LastName = "Doe", PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginUserRequest("test@test.com", "WrongPassword!");

            var result = await _authService.LoginAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid email or password.");
        }


        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnSuccess_WhenTokenIsValid()
        {
            var user = new User { Email = "test@test.com", FirstName = "John", LastName = "Doe", PasswordHash = "hash" };
            var oldRefreshToken = new RefreshToken { Token = "old_token", ExpiresAt = DateTime.UtcNow.AddDays(1), UserId = user.Id, User = user };

            _context.Users.Add(user);
            _context.RefreshTokens.Add(oldRefreshToken);
            await _context.SaveChangesAsync();

            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<User>()))
                .Returns(("new_jwt_token", DateTime.UtcNow.AddHours(1)));
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns("new_refresh_token");

            var request = new RefreshTokenRequest("old_token");

            var result = await _authService.RefreshTokenAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.RefreshToken.Should().Be("new_refresh_token");

            var revokedToken = await _context.RefreshTokens.FirstAsync(rt => rt.Token == "old_token");
            revokedToken.RevokedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnUnauthorized_WhenTokenIsExpired()
        {
            var oldRefreshToken = new RefreshToken { Token = "old_token", ExpiresAt = DateTime.UtcNow.AddDays(-1), UserId = Guid.NewGuid() };
            _context.RefreshTokens.Add(oldRefreshToken);
            await _context.SaveChangesAsync();

            var request = new RefreshTokenRequest("old_token");
            var result = await _authService.RefreshTokenAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid or expired refresh token.");
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnUnauthorized_WhenTokenIsRevoked()
        {
            var oldRefreshToken = new RefreshToken { Token = "old_token", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow, UserId = Guid.NewGuid() };
            _context.RefreshTokens.Add(oldRefreshToken);
            await _context.SaveChangesAsync();

            var request = new RefreshTokenRequest("old_token");
            var result = await _authService.RefreshTokenAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid or expired refresh token.");
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnUnauthorized_WhenTokenDoesNotExist()
        {
            var request = new RefreshTokenRequest("non_existent_token");
            var result = await _authService.RefreshTokenAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid or expired refresh token.");
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldReturnSuccess_AndRevokeToken()
        {
            var token = new RefreshToken { Token = "valid_token", ExpiresAt = DateTime.UtcNow.AddDays(1), UserId = Guid.NewGuid() };
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();

            var request = new RevokeTokenRequest("valid_token");
            var result = await _authService.RevokeTokenAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var revokedToken = await _context.RefreshTokens.FirstAsync();
            revokedToken.RevokedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldReturnSuccess_EvenIfTokenDoesNotExist()
        {
            var request = new RevokeTokenRequest("non_existent_token");
            var result = await _authService.RevokeTokenAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }


        [Fact]
        public async Task LoginWithGoogleAsync_ShouldReturnUnauthorized_WhenTokenIsInvalid()
        {
            var request = new GoogleLoginRequest("invalid_google_jwt");

            var result = await _authService.LoginWithGoogleAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid Google token.");
        }

        private class FaultyDbContext : AppDbContext
        {
            public FaultyDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                throw new DbUpdateException("Simulated race condition");
            }
        }
    }
}
