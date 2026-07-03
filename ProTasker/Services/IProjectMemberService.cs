using ProTasker.Common;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.DTOs.Responses.ProjectMember;

namespace ProTasker.Services
{
    public interface IProjectMemberService
    {
        public Task<Result<List<ProjectMemberResponse>>> GetAllAsync(CancellationToken cancellationToken);
        public Task<Result<ProjectMemberResponse>> GetByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
        public Task<Result<ProjectMemberResponse>> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken);
        public Task<Result> DeleteByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
    }
}
