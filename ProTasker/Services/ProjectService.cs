using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Data;
using ProTasker.DTOs.Requests.Project;
using ProTasker.DTOs.Responses.Project;
using ProTasker.Models;

namespace ProTasker.Services
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public ProjectService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ProjectListItemResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ProjectTo<ProjectListItemResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProjectDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            ProjectDetailsResponse? result = await _context.Projects
                .AsNoTracking()
                .ProjectTo<ProjectDetailsResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return result;
        }

        public async Task<ProjectDetailsResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
        {
            Project project = new Project
            {
                Name = request.Name,
                Description = request.Description
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ProjectDetailsResponse>(project);
        }

        public async Task<ProjectDetailsResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
        {
            Project? project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (project == null)
                return null;

            if (request.Name is not null)
                project.Name = request.Name;

            if (request.Description is not null)
                project.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ProjectDetailsResponse>(project);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Project? project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (project == null)
                return false;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
