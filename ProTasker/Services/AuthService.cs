using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Models;

namespace ProTasker.Services
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
            AuthResponse response = new AuthResponse(tokenData.Token, tokenData.ExpiresAt, _mapper.Map<UserResponse>(user));

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
            AuthResponse response = new AuthResponse(tokenData.Token, tokenData.ExpiresAt, _mapper.Map<UserResponse>(user));

            _logger.LogInformation("User {UserId} logged in successfully.", user.Id);
            return Result<AuthResponse>.Success(response);
        }
    }
}
