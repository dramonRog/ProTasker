using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.Models;
using ProTasker.Services.Interfaces;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace ProTasker.Services.Implementations
{
    public class ProjectAccessService : IProjectAccessService
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<ProjectAccessService> _logger;

        public ProjectAccessService(AppDbContext context, IUserContextService userContextService, ILogger<ProjectAccessService> logger)
        {
            _context = context;
            _userContextService = userContextService;
            _logger = logger;
        }

        public async Task<Result> EnsureMemberAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            bool isMember = await _context.Projects.AnyAsync(p => p.Id == projectId && p.ProjectMembers.Any(pm => pm.UserId == currentId), cancellationToken);
            
            if (!isMember)
            {
                _logger.LogWarning("User {UserId} attempted to access project {ProjectId} without being a member, or the project was deleted.", currentId, projectId);
                return Result.Forbidden("You are not a member of this project, or it has been deleted.");
            }

            return Result.Success();
        }

        public async Task<Result<ProjectMember>> EnsureAdminAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            ProjectMember? admin = await _context.Projects
                .Where(p => p.Id == projectId)
                .SelectMany(p => p.ProjectMembers.Where(pm => pm.UserId == currentId))
                .FirstOrDefaultAsync(cancellationToken);

            if (admin == null)
                return Result<ProjectMember>.Forbidden("You are not a member of this project, or it has been deleted.");

            if (admin.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted admin action in project {ProjectId}.", currentId, projectId);
                return Result<ProjectMember>.Forbidden("Only administrators can perform this action.");
            }

            return Result<ProjectMember>.Success(admin);
        }
    }
}
