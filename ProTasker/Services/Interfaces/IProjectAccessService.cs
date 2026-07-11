using ProTasker.Common;
using ProTasker.Models;

namespace ProTasker.Services.Interfaces
{
    public interface IProjectAccessService
    {
        Task<Result> EnsureMemberAsync(Guid projectId, CancellationToken cancellationToken);
        Task<Result<ProjectMember>> EnsureAdminAsync(Guid projectId, CancellationToken cancellationToken);
    }
}
