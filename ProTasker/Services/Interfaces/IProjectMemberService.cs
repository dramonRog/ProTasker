using ProTasker.Common;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.DTOs.Responses.ProjectMember;
using ProTasker.Pagination;

namespace ProTasker.Services.Interfaces
{
    public interface IProjectMemberService
    {
        public Task<Result<PagedResult<ProjectMemberResponse>>> GetAllAsync(Guid projectId, PaginationQuery pagination, CancellationToken cancellationToken);
        public Task<Result<ProjectMemberResponse>> GetByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
        public Task<Result<ProjectMemberResponse>> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken);
        public Task<Result<ProjectMemberResponse>> ChangeProjectMemberRoleAsync(Guid userId, Guid projectId, ChangeProjectMemberRole request, CancellationToken cancellationToken);
        public Task<Result> DeleteByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
    }
}
