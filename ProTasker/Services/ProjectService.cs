using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
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

        public async Task<Result<List<ProjectListItemResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            List<ProjectListItemResponse> projectsList = await _context.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ProjectTo<ProjectListItemResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<ProjectListItemResponse>>.Success(projectsList);
        }

        public async Task<Result<ProjectDetailsResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            ProjectDetailsResponse? result = await _context.Projects
                .AsNoTracking()
                .ProjectTo<ProjectDetailsResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return result == null ? Result<ProjectDetailsResponse>.NotFound("Project was not found.") : Result<ProjectDetailsResponse>.Success(result);
        }

        public async Task<Result<ProjectDetailsResponse>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
        {
            Project project = new Project
            {
                Name = request.Name,
                Description = request.Description
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(cancellationToken);

            ProjectDetailsResponse projectItem = _mapper.Map<ProjectDetailsResponse>(project);
            return Result<ProjectDetailsResponse>.Success(projectItem);
        }

        public async Task<Result<ProjectDetailsResponse>> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
        {
            Project? project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (project == null)
                return Result<ProjectDetailsResponse>.NotFound("Project was not found.");

            if (request.Name is not null)
                project.Name = request.Name;

            if (request.Description is not null)
                project.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);

            ProjectDetailsResponse response = _mapper.Map<ProjectDetailsResponse>(project);
            return Result<ProjectDetailsResponse>.Success(response);
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Project? project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (project == null)
                return Result.NotFound("Project was not found.");

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
