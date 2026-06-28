using ProTasker.DTOs.Requests.Project;
using ProTasker.DTOs.Responses.Project;

namespace ProTasker.Services
{
    public interface IProjectService
    {
        Task<List<ProjectListItemResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<ProjectDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<ProjectDetailsResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);
        Task<ProjectDetailsResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    }
}
