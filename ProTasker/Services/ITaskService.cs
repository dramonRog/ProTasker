using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;

namespace ProTasker.Services
{
    public interface ITaskService
    {
        Task<List<TaskResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<TaskResponse?> GetByIdAsync(Guid Id, CancellationToken cancellationToken);
        Task<TaskResponse> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken);
        Task<TaskResponse?> UpdateAsync(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken);
        Task<TaskResponse?> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken);
        Task<TaskResponse?> ChangeTaskStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteByIdAsync(Guid Id, CancellationToken cancellationToken);
    }
}
