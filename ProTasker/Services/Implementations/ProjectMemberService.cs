using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.DTOs.Responses.ProjectMember;
using ProTasker.Models;
using ProTasker.Models.Enums;
using ProTasker.Pagination;
using ProTasker.Services.Interfaces;

namespace ProTasker.Services.Implementations
{
    public class ProjectMemberService : IProjectMemberService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;
        private readonly IProjectAccessService _projectAccessService;
        private readonly ILogger<ProjectMemberService> _logger;

        public ProjectMemberService(AppDbContext context, IMapper mapper, IUserContextService userContextService, IProjectAccessService projectAccessService, ILogger<ProjectMemberService> logger)
        {
            _context = context;
            _mapper = mapper;
            _userContextService = userContextService;
            _projectAccessService = projectAccessService;
            _logger = logger;
        }
        public async Task<Result<PagedResult<ProjectMemberResponse>>> GetAllAsync(Guid projectId, PaginationQuery pagination, CancellationToken cancellationToken)
        {
            Result access = await _projectAccessService.EnsureMemberAsync(projectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<PagedResult<ProjectMemberResponse>>.Forbidden(access.Error);

            IQueryable<ProjectMember> query = _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.ProjectId == projectId)
                .OrderBy(pm => pm.AddedAt);

            int totalCount = await query.CountAsync(cancellationToken);
            int skipAmount = (pagination.PageNumber - 1) * pagination.PageSize;

            List<ProjectMemberResponse> projectMemberItems = await query
                .Skip(skipAmount)
                .ProjectTo<ProjectMemberResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<PagedResult<ProjectMemberResponse>>.Success(new PagedResult<ProjectMemberResponse>(projectMemberItems, totalCount, pagination.PageNumber, pagination.PageSize));
        }

        public async Task<Result<ProjectMemberResponse>> GetByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            Result access = await _projectAccessService.EnsureMemberAsync(projectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<ProjectMemberResponse>.Forbidden(access.Error);

            var result = await _context.ProjectMembers
                .AsNoTracking()
                    .Include(pm => pm.User)
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            return result == null ? Result<ProjectMemberResponse>.NotFound("Project member was not found.") : Result<ProjectMemberResponse>.Success(_mapper.Map<ProjectMemberResponse>(result));
        }

        public async Task<Result<ProjectMemberResponse>> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            Result<ProjectMember> adminResult = await _projectAccessService.EnsureAdminAsync(projectId, cancellationToken);
            if (!adminResult.IsSuccess)
                return Result<ProjectMemberResponse>.Forbidden(adminResult.Error);

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null)
                return Result<ProjectMemberResponse>.NotFound("User was not found.");

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
            return Result<ProjectMemberResponse>.Success(_mapper.Map<ProjectMemberResponse>(projectMember));
        }

        public async Task<Result<ProjectMemberResponse>> ChangeProjectMemberRoleAsync(Guid userId, Guid projectId, ChangeProjectMemberRole request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            Result<ProjectMember> adminResult = await _projectAccessService.EnsureAdminAsync(projectId, cancellationToken);
            if (!adminResult.IsSuccess)
                return Result<ProjectMemberResponse>.Forbidden(adminResult.Error);

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

            ProjectMember? currentMember = await _context.Projects
                .Where(p => p.Id == projectId)
                .SelectMany(p => p.ProjectMembers.Where(pm => pm.UserId == currentId))
                .FirstOrDefaultAsync(cancellationToken);

            if (currentMember == null)
                return Result.Forbidden("You are not a member of this project, or it has been deleted.");

            if (currentMember.Role != ProjectRole.Admin && userId != currentId)
            {
                _logger.LogWarning("User {UserId} attempted to remove user {TargetUserId} from project {ProjectId} without Admin role.", currentId, userId, projectId);
                return Result.Forbidden("Only administrators can remove other members from the project.");
            }

            ProjectMember? memberToRemove = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            if (memberToRemove == null)
                return Result.NotFound("Project member was not found.");

            if (memberToRemove.Role == ProjectRole.Admin &&
                !await _context.ProjectMembers.AnyAsync(pm => pm.UserId != userId && pm.ProjectId == projectId && pm.Role == ProjectRole.Admin, cancellationToken))
            {
                _logger.LogWarning("Blocked attempt to remove the last administrator {UserId} from project {ProjectId}.", userId, projectId);
                return Result.Conflict("Can't remove the last administrator of the project.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.ProjectMembers.Remove(memberToRemove);

                await _context.TaskItems
                    .Where(t => t.ProjectId == projectId && t.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.UserId, (Guid?)null), cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to remove user {UserId} from project {ProjectId} and unassign their tasks.", userId, projectId);
                throw;
            }

            _logger.LogInformation("User {RequesterId} removed user {UserId} from project {ProjectId}. All their assigned tasks were unassigned.", currentId, userId, projectId);
            return Result.Success();
        }
    }
}
