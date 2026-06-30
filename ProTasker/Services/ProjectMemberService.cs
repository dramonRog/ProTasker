using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
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
        public async Task<List<ProjectMemberResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.ProjectMembers
                .AsNoTracking()
                .ProjectTo<ProjectMemberResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProjectMemberResponse?> GetByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            return await _context.ProjectMembers
                .AsNoTracking()
                .ProjectTo<ProjectMemberResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);
        }

        public async Task<ProjectMemberResponse?> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return null;

            Project? project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project == null)
                return null;

            ProjectMember projectMember = new ProjectMember 
            {
                UserId = request.UserId,
                ProjectId = projectId,
                User = user,
                Project = project
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ProjectMemberResponse>(projectMember);
        }

        public async Task<bool> DeleteByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            ProjectMember? projectMember = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken);

            if (projectMember == null)
                return false;

            _context.ProjectMembers.Remove(projectMember);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
