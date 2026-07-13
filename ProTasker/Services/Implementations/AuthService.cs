using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Models;
using ProTasker.Models.Entities;
using ProTasker.Services.Interfaces;

namespace ProTasker.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IMapper mapper, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _context = context;
            _mapper = mapper;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<AuthResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
        {
            string normalizedEmail = request.Email.ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
            {
                _logger.LogWarning("Registration failed: Email {Email} is already in use.", normalizedEmail);
                return Result<AuthResponse>.Conflict("Email is already in use.");
            }

            User user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);

            // Somebody can take email at this moment
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _logger.LogWarning("Registration race condition detected: Email {Email} was taken during save.", normalizedEmail);
                return Result<AuthResponse>.Conflict("Email is already in use.");
            }


            (string Token, DateTime ExpiresAt) tokenData = _tokenService.CreateToken(user);
            string refreshTokenString = _tokenService.GenerateRefreshToken();

            RefreshToken refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            AuthResponse response = new AuthResponse(tokenData.Token, refreshTokenString, tokenData.ExpiresAt, _mapper.Map<UserResponse>(user));

            _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);
            return Result<AuthResponse>.Success(response);
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken)
        {
            string normalizedEmail = request.Email.ToLowerInvariant();

            User? user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", normalizedEmail);
                return Result<AuthResponse>.Unauthorized("Invalid email or password.");
            }

            (string Token, DateTime ExpiresAt) tokenData = _tokenService.CreateToken(user);

            string refreshTokenString = _tokenService.GenerateRefreshToken();

            RefreshToken refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            AuthResponse response = new AuthResponse(tokenData.Token, refreshTokenString, tokenData.ExpiresAt, _mapper.Map<UserResponse>(user));

            _logger.LogInformation("User {UserId} logged in successfully.", user.Id);
            return Result<AuthResponse>.Success(response);
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            RefreshToken? refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                _logger.LogWarning("Attempted to use invalid or expired refresh token.");
                return Result<AuthResponse>.Unauthorized("Invalid or expired refresh token.");
            }

            refreshToken.RevokedAt = DateTime.UtcNow;

            (string Token, DateTime ExpiresAt) tokenData = _tokenService.CreateToken(refreshToken.User);
            string newRefreshTokenString = _tokenService.GenerateRefreshToken();

            RefreshToken newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = refreshToken.UserId
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully refreshed tokens.", refreshToken.UserId);

            AuthResponse authResponse = new AuthResponse(tokenData.Token, newRefreshTokenString, tokenData.ExpiresAt, _mapper.Map<UserResponse>(refreshToken.User));
            return Result<AuthResponse>.Success(authResponse);
        }

        public async Task<Result> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken)
        {
            RefreshToken? refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (refreshToken == null || !refreshToken.IsActive)
                return Result.Success();

            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refresh token was successfully revoked (User ID: {UserId}).", refreshToken.UserId);
            return Result.Success();
        }
    }
}
