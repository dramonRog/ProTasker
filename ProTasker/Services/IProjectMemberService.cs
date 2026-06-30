using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.DTOs.Responses.ProjectMember;

namespace ProTasker.Services
{
    public interface IProjectMemberService
    {
        public Task<List<ProjectMemberResponse>> GetAllAsync(CancellationToken cancellationToken);
        public Task<ProjectMemberResponse?> GetByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
        public Task<ProjectMemberResponse> AddProjectMemberToProjectAsync(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken);
        public Task<bool> DeleteByIdAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
    }
}
