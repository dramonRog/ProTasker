using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.DTOs.Responses.ProjectMember;
using ProTasker.Models;

namespace ProTasker.Services
{
    public class ProjectMemberService : IProjectMemberService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<ProjectMemberService> _logger;

        public ProjectMemberService(AppDbContext context, IMapper mapper, IUserContextService userContextService, ILogger<ProjectMemberService> logger)
        {
            _context = context;
            _mapper = mapper;
            _userContextService = userContextService;
            _logger = logger;
        }
        public async Task<Result<List<ProjectMemberResponse>>> GetAllAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();
            if (!await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId, cancellationToken))
            {
                _logger.LogWarning("User {UserId} attempted to list members of project {ProjectId} without being a member.", currentUserId, projectId);
                return Result<List<ProjectMemberResponse>>.Forbidden("You are not a member of this project.");
            }

            var result = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.ProjectId == projectId)
                .ProjectTo<ProjectMemberResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<ProjectMemberResponse>>.Success(result);
        }

        public async Task<Result<ProjectMemberResponse>> GetByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            if (!await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentId, cancellationToken))
            {
                _logger.LogWarning("User {UserId} attempted to get member details in project {ProjectId} without being a member.", currentId, projectId);
                return Result<ProjectMemberResponse>.Forbidden("You are not a member of this project.");
            }

            var result = await _context.ProjectMembers
                .AsNoTracking()
                    .Include(pm => pm.User)
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            return result == null ? Result<ProjectMemberResponse>.NotFound("Project member was not found.") : Result<ProjectMemberResponse>.Success(_mapper.Map<ProjectMemberResponse>(result));
        }

        public async Task<Result<ProjectMemberResponse>> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (member == null)
                return Result<ProjectMemberResponse>.Forbidden("You are not a member of this project.");

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null)
                return Result<ProjectMemberResponse>.NotFound("User was not found.");

            if (!await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
                return Result<ProjectMemberResponse>.NotFound("Project was not found.");

            if (member.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to add user {TargetUserId} to project {ProjectId} without Admin role.", currentId, request.UserId, projectId);
                return Result<ProjectMemberResponse>.Forbidden("Only administrators can add users to the project.");
            }

            if (await _context.ProjectMembers.AnyAsync(pm => pm.UserId == request.UserId && pm.ProjectId == projectId, cancellationToken))
                return Result<ProjectMemberResponse>.Conflict("User is already a member of this project.");

            ProjectMember projectMember = new ProjectMember 
            {
                UserId = request.UserId,
                ProjectId = projectId,
                Role = request.Role,
                User = user
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {AdminId} successfully added user {UserId} to project {ProjectId} with role {Role}.", currentId, request.UserId, projectId, request.Role);

            ProjectMemberResponse response = _mapper.Map<ProjectMemberResponse>(projectMember);
            return Result<ProjectMemberResponse>.Success(response);
        }

        public async Task<Result<ProjectMemberResponse>> ChangeProjectMemberRoleAsync(Guid userId, Guid projectId, ChangeProjectMemberRole request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            ProjectMember? admin = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentUserId && pm.ProjectId == projectId, cancellationToken);
            if (admin == null)
                return Result<ProjectMemberResponse>.Forbidden("You are not a member of this project.");

            if (admin.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to change role of user {TargetUserId} in project {ProjectId} without Admin role.", currentUserId, userId, projectId);
                return Result<ProjectMemberResponse>.Forbidden("Only administrators can change member role.");
            }

            ProjectMember? projectMember = await _context.ProjectMembers
                .Include(pm => pm.User)
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken);

            if (projectMember == null)
                return Result<ProjectMemberResponse>.NotFound("Member was not found.");

            if (projectMember.Role == request.Role)
                return Result<ProjectMemberResponse>.Success(_mapper.Map<ProjectMemberResponse>(projectMember));

            if (projectMember.Role == ProjectRole.Admin && request.Role != ProjectRole.Admin 
                && !await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId != userId && pm.Role == ProjectRole.Admin, cancellationToken))
                    return Result<ProjectMemberResponse>.Conflict("In the project must be at least one administrator.");

            projectMember.Role = request.Role;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {AdminId} changed role of user {UserId} in project {ProjectId} to {NewRole}.", currentUserId, userId, projectId, request.Role);
            return Result<ProjectMemberResponse>.Success(_mapper.Map<ProjectMemberResponse>(projectMember));
        }

        public async Task<Result> DeleteByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (member == null)
                return Result.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin && userId != currentId)
            {
                _logger.LogWarning("User {UserId} attempted to remove user {TargetUserId} from project {ProjectId} without Admin role.", currentId, userId, projectId);
                return Result.Forbidden("Only administrators can remove other members from the project.");
            }

            ProjectMember? memberToRemove = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            if (memberToRemove == null)
                return Result.NotFound("Project member was not found.");

            if (memberToRemove.Role == ProjectRole.Admin &&
                !await _context.ProjectMembers.AnyAsync(pm => pm.UserId != userId && pm.ProjectId == projectId && pm.Role == ProjectRole.Admin))
            {
                _logger.LogWarning("Blocked attempt to remove the last administrator {UserId} from project {ProjectId}.", userId, projectId);
                return Result.Conflict("Can't remove the last administrator of the project.");
            }

            _context.ProjectMembers.Remove(memberToRemove);

            await _context.TaskItems
                .Where(t => t.ProjectId == projectId && t.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UserId, (Guid?)null));

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {RequesterId} removed user {UserId} from project {ProjectId}. All their assigned tasks were unassigned.", currentId, userId, projectId);
            return Result.Success();
        }
    }
}
