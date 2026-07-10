using ProTasker.Common;
using ProTasker.DTOs.Requests.Project;
using ProTasker.DTOs.Responses.Project;

namespace ProTasker.Services.Interfaces
{
    public interface IProjectService
    {
        Task<Result<List<ProjectListItemResponse>>> GetAllAsync(CancellationToken cancellationToken);
        Task<Result<ProjectDetailsResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<Result<ProjectDetailsResponse>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);
        Task<Result<ProjectDetailsResponse>> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
    }
}
