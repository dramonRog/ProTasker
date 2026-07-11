using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.Project;
using ProTasker.DTOs.Responses.Project;
using ProTasker.Models;
using ProTasker.Services.Interfaces;

namespace ProTasker.Services.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly IProjectAccessService _projectAccessService;
        private readonly IMapper _mapper;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(AppDbContext context, IUserContextService userContextService, IProjectAccessService projectAccessService, IMapper mapper, ILogger<ProjectService> logger)
        {
            _context = context;
            _userContextService = userContextService;
            _projectAccessService = projectAccessService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<ProjectListItemResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            List<ProjectListItemResponse> projectsList = await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProjectMembers.Any(pm => pm.UserId ==  currentUserId))
                .ProjectTo<ProjectListItemResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<ProjectListItemResponse>>.Success(projectsList);
        }

        public async Task<Result<ProjectDetailsResponse>> GetByIdAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Result access = await _projectAccessService.EnsureMemberAsync(projectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<ProjectDetailsResponse>.Forbidden(access.Error);

            Project? project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project == null)
                return Result<ProjectDetailsResponse>.NotFound("Project was not found.");

            return Result<ProjectDetailsResponse>.Success(_mapper.Map<ProjectDetailsResponse>(project));
        }

        public async Task<Result<ProjectDetailsResponse>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            Project project = new Project
            {
                Name = request.Name,
                Description = request.Description
            };

            User user = await _context.Users.FirstAsync(u => u.Id == currentUserId, cancellationToken);

            ProjectMember admin = new ProjectMember
            {
                Role = ProjectRole.Admin,
                User = user,
                Project = project,
            };

            Board[] defaultBoards =
            [
                new Board { Name = "To Do", OrderIndex = 0, Project = project },
                new Board { Name = "In Progress", OrderIndex = 1, Project = project },
                new Board { Name = "Done", OrderIndex = 2, Project = project }
            ];

            _context.Projects.Add(project);
            _context.ProjectMembers.Add(admin);
            _context.Boards.AddRange(defaultBoards);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully created project {ProjectId} ('{ProjectName}').", currentUserId, project.Id, project.Name);
            return Result<ProjectDetailsResponse>.Success(_mapper.Map<ProjectDetailsResponse>(project));
        }

        public async Task<Result<ProjectDetailsResponse>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            Result<ProjectMember> adminResult = await _projectAccessService.EnsureAdminAsync(projectId, cancellationToken);
            if (!adminResult.IsSuccess)
                return Result<ProjectDetailsResponse>.Forbidden(adminResult.Error);

            Project? project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project == null)
                return Result<ProjectDetailsResponse>.NotFound("Project was not found.");

            if (request.Name is not null)
                project.Name = request.Name;

            if (request.Description is not null)
                project.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {UserId} successfully updated project {ProjectId}.", currentUserId, projectId);
            return Result<ProjectDetailsResponse>.Success(_mapper.Map<ProjectDetailsResponse>(project));
        }

        public async Task<Result> DeleteAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            Result<ProjectMember> adminResult = await _projectAccessService.EnsureAdminAsync(projectId, cancellationToken);
            if (!adminResult.IsSuccess)
                return Result.Forbidden(adminResult.Error);

            Project? project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
            if (project == null)
                return Result.NotFound("Project was not found.");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.Projects.Remove(project);

                await _context.TaskItems
                    .Where(t => t.ProjectId == projectId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.IsDeleted, true)
                        .SetProperty(t => t.DeletedAt, DateTime.UtcNow), cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Admin {UserId} soft-deleted project {ProjectId} and its tasks.", currentId, projectId);
            return Result.Success();
        }
    }
}
