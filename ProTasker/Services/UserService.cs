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
        private readonly IUserContextService _userContextService;
        private readonly AppDbContext _context;

        public UserService(IMapper mapper, IUserContextService userContextService, AppDbContext context)
        {
            _mapper = mapper;
            _userContextService = userContextService;
            _context = context;
        }

        public async Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            var myProjectIds = _context.ProjectMembers
                .Where(pm => pm.UserId == currentUserId)
                .Select(pm => pm.ProjectId);
            
            var usersList = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId || u.ProjectMembers.Any(pm => myProjectIds.Contains(pm.ProjectId)))
                .ProjectTo<UserResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<UserResponse>>.Success(usersList);
        }

        public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            var myProjectsIds = _context.ProjectMembers
                .Where(pm => pm.UserId == currentUserId)
                .Select(pm => pm.ProjectId);

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId || u.ProjectMembers.Any(pm => myProjectsIds.Contains(pm.ProjectId)))
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);


            return user == null ? Result<UserResponse>.NotFound("User was not found.") : Result<UserResponse>.Success(_mapper.Map<UserResponse>(user));
        }

        public async Task<Result<UserResponse>> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

            if (user == null)
                return Result<UserResponse>.NotFound("User was not found.");

            if (request.FirstName is not null)
                user.FirstName = request.FirstName;

            if (request.LastName is not null)
                user.LastName = request.LastName;

            if (request.Email is not null)
            {
                string normalizedEmail = request.Email.ToLowerInvariant();

                if (await _context.Users.AnyAsync(u => u.Id != currentUserId && u.Email == normalizedEmail, cancellationToken))
                    return Result<UserResponse>.Conflict("Email is already in use.");

                user.Email = normalizedEmail;
            }

            if (request.Password is not null)
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // for case, when another user had took this email before you did
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Result<UserResponse>.Conflict("Email is already in use.");
            }
            var userResponse = _mapper.Map<UserResponse>(user);
            return Result<UserResponse>.Success(userResponse);
        }

        public async Task<Result> DeleteUserAsync(CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

            if (user == null)
                return Result.NotFound("User was not found.");

            Project? project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectMembers.Any(pm => pm.UserId == currentUserId && pm.Role == ProjectRole.Admin)
                && p.ProjectMembers.Count(pm => pm.Role == ProjectRole.Admin) == 1);

            if (project != null)
                return Result.Conflict("There is at least one project, where only you are an admin. You must remove it or assign another user as the admin.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
