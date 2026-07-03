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

        public ProjectMemberService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<Result<List<ProjectMemberResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            var result = await _context.ProjectMembers
                .AsNoTracking()
                .ProjectTo<ProjectMemberResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<ProjectMemberResponse>>.Success(result);
        }

        public async Task<Result<ProjectMemberResponse>> GetByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _context.ProjectMembers
                .AsNoTracking()
                .ProjectTo<ProjectMemberResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            return result == null ? Result<ProjectMemberResponse>.NotFound("Project member was not found.") : Result<ProjectMemberResponse>.Success(result);
        }

        public async Task<Result<ProjectMemberResponse>> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
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
                Project = project
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync(cancellationToken);

            ProjectMemberResponse response = _mapper.Map<ProjectMemberResponse>(projectMember);
            return Result<ProjectMemberResponse>.Success(response);
        }

        public async Task<Result> DeleteByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            ProjectMember? projectMember = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            if (projectMember == null)
                return Result.NotFound("Project member was not found.");

            _context.ProjectMembers.Remove(projectMember);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
