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
        private readonly IUserContextService _userContextService;
        private readonly IMapper _mapper;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(AppDbContext context, IUserContextService userContextService, IMapper mapper, ILogger<ProjectService> logger)
        {
            _context = context;
            _userContextService = userContextService;
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
            Guid currentUserId = _userContextService.GetCurrentUserId();

            if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == currentUserId && pm.ProjectId == projectId, cancellationToken))
            {
                _logger.LogWarning("User {UserId} attempted to access project {ProjectId} without being a member.", currentUserId, projectId);
                return Result<ProjectDetailsResponse>.Forbidden("You are not a member of this project.");
            }

            Project? project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project == null)
                return Result<ProjectDetailsResponse>.NotFound("Project was not found.");

            ProjectDetailsResponse result = _mapper.Map<ProjectDetailsResponse>(project);

            return Result<ProjectDetailsResponse>.Success(result);
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

            _context.Projects.Add(project);
            _context.ProjectMembers.Add(admin);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully created project {ProjectId} ('{ProjectName}').", currentUserId, project.Id, project.Name);

            ProjectDetailsResponse projectItem = _mapper.Map<ProjectDetailsResponse>(project);
            return Result<ProjectDetailsResponse>.Success(projectItem);
        }

        public async Task<Result<ProjectDetailsResponse>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();

            Project? project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
            if (project == null)
                return Result<ProjectDetailsResponse>.NotFound("Project was not found.");

            ProjectMember? projectMember = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId, cancellationToken);
            if (projectMember == null)
            {
                _logger.LogWarning("User {UserId} attempted to update project {ProjectId} but is not a member.", currentUserId, projectId);
                return Result<ProjectDetailsResponse>.Forbidden("You are not a member of this project.");
            }

            if (projectMember.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to update project {ProjectId} but lacks Admin role.", currentUserId, projectId);
                return Result<ProjectDetailsResponse>.Forbidden("Only administrators can update project data.");
            }

            if (request.Name is not null)
                project.Name = request.Name;

            if (request.Description is not null)
                project.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully updated project {ProjectId}.", currentUserId, projectId);

            ProjectDetailsResponse response = _mapper.Map<ProjectDetailsResponse>(project);
            return Result<ProjectDetailsResponse>.Success(response);
        }

        public async Task<Result> DeleteAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Project? project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
            if (project == null)
                return Result.NotFound("Project was not found.");

            Guid currentUserId = _userContextService.GetCurrentUserId();
            ProjectMember? projectMember = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId, cancellationToken);

            if (projectMember == null)
            {
                _logger.LogWarning("User {UserId} attempted to delete project {ProjectId} but is not a member.", currentUserId, projectId);
                return Result.Forbidden("You are not a member of this project.");
            }

            if (projectMember.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to delete project {ProjectId} but lacks Admin role.", currentUserId, projectId);
                return Result.Forbidden("Only administrators can delete project.");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} permanently deleted project {ProjectId}.", currentUserId, projectId);

            return Result.Success();
        }
    }
}
