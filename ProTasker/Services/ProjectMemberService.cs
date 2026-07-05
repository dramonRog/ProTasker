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

        public ProjectMemberService(AppDbContext context, IMapper mapper, IUserContextService userContextService)
        {
            _context = context;
            _mapper = mapper;
            _userContextService = userContextService;
        }
        public async Task<Result<List<ProjectMemberResponse>>> GetAllAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();
            if (!await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId, cancellationToken))
                return Result<List<ProjectMemberResponse>>.Forbidden("You are not a member of this project.");

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
                return Result<ProjectMemberResponse>.Forbidden("You are not a member of this project.");

            var result = await _context.ProjectMembers
                .AsNoTracking()
                .ProjectTo<ProjectMemberResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            return result == null ? Result<ProjectMemberResponse>.NotFound("Project member was not found.") : Result<ProjectMemberResponse>.Success(result);
        }

        public async Task<Result<ProjectMemberResponse>> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (member == null)
                return Result<ProjectMemberResponse>.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
                return Result<ProjectMemberResponse>.Forbidden("Only administrators can add users to the project.");

            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return Result<ProjectMemberResponse>.NotFound("User was not found.");

            Project? project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project == null)
                return Result<ProjectMemberResponse>.NotFound("Project was not found.");

            if (await _context.ProjectMembers.AnyAsync(pm => pm.UserId == request.UserId && pm.ProjectId == projectId, cancellationToken))
                return Result<ProjectMemberResponse>.Conflict("User is already a member of this project.");

            ProjectMember projectMember = new ProjectMember 
            {
                UserId = request.UserId,
                ProjectId = projectId,
                User = user,
                Project = project,
                Role = request.Role
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync(cancellationToken);

            ProjectMemberResponse response = _mapper.Map<ProjectMemberResponse>(projectMember);
            return Result<ProjectMemberResponse>.Success(response);
        }

        public async Task<Result> DeleteByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (member == null)
                return Result.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin && userId != currentId)
                return Result.Forbidden("Only administrators can remove other members from the project.");

            ProjectMember? memberToRemove = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            if (memberToRemove == null)
                return Result.NotFound("Project member was not found.");

            if (memberToRemove.Role == ProjectRole.Admin &&
                !await _context.ProjectMembers.AnyAsync(pm => pm.UserId != userId && pm.ProjectId == projectId && pm.Role == ProjectRole.Admin))
                return Result.Conflict("Can't remove the last administrator of the project.");

            _context.ProjectMembers.Remove(memberToRemove);

            var userTasks = await _context.TaskItems
                .Where(t => t.ProjectId == projectId && t.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (var task in userTasks)
                task.UserId = null;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
