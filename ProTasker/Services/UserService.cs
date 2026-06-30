using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
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

        public async Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Users
                .AsNoTracking()
                .ProjectTo<UserResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Users
                .AsNoTracking()
                .ProjectTo<UserResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<UserResponse?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
                return null;

            if (request.FirstName is not null)
                user.FirstName = request.FirstName;

            if (request.LastName is not null)
                user.LastName = request.LastName;

            if (request.Email is not null)
            {
                string normalizedEmail = request.Email.ToLowerInvariant();

                if (await _context.Users.AnyAsync(u => u.Id != id && u.Email == normalizedEmail, cancellationToken))
                    return null;

                user.Email = normalizedEmail;
            }

            if (request.Password is not null)
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<UserResponse>(user);
        }

        public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
