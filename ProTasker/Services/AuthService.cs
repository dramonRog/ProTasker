using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

        public AuthService(AppDbContext context, IMapper mapper, ITokenService tokenService)
        {
            _context = context;
            _mapper = mapper;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
        {
            string normalizedEmail = request.Email.ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
                return null;

            User user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            (string Token, DateTime expiresAt) tokenData = _tokenService.CreateToken(user);

            return new AuthResponse(tokenData.Token, tokenData.expiresAt, _mapper.Map<UserResponse>(user));
        }

        public async Task<AuthResponse?> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken)
        {
            string normalizedEmail = request.Email.ToLowerInvariant();

            User? user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            (string Token, DateTime expiresAt) tokenData = _tokenService.CreateToken(user);

            return new AuthResponse(tokenData.Token, tokenData.expiresAt, _mapper.Map<UserResponse>(user));
        }
    }
}
