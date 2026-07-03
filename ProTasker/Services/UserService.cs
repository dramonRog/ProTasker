using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Models;

namespace ProTasker.Services
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public UserService(IMapper mapper, AppDbContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        public async Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            var usersList = await _context.Users
                .AsNoTracking()
                .ProjectTo<UserResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<UserResponse>>.Success(usersList);
        }

        public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsNoTracking()
                .ProjectTo<UserResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            return user == null ? Result<UserResponse>.NotFound("User was not found.") : Result<UserResponse>.Success(user);
        }

        public async Task<Result<UserResponse>> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
                return Result<UserResponse>.NotFound("User was not found.");

            if (request.FirstName is not null)
                user.FirstName = request.FirstName;

            if (request.LastName is not null)
                user.LastName = request.LastName;

            if (request.Email is not null)
            {
                string normalizedEmail = request.Email.ToLowerInvariant();

                if (await _context.Users.AnyAsync(u => u.Id != id && u.Email == normalizedEmail, cancellationToken))
                    return Result<UserResponse>.Conflict("Email is already in use.");

                user.Email = normalizedEmail;
            }

            if (request.Password is not null)
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _context.SaveChangesAsync(cancellationToken);

            var userResponse = _mapper.Map<UserResponse>(user);
            return Result<UserResponse>.Success(userResponse);
        }

        public async Task<Result> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
                return Result.NotFound("User was not found.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
