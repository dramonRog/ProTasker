using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;

namespace ProTasker.Services
{
    public interface ITaskService
    {
        Task<Result<List<TaskResponse>>> GetAllProjectTasksAsync(Guid projectId, CancellationToken cancellationToken);
        Task<Result<List<TaskResponse>>> GetAllUserTasksAsync(Guid? projectId, Guid userId, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> GetByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> UpdateAsync(Guid projectId, Guid taskId, UpdateTaskItemRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> AssignTaskAsync(Guid projectId, Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> ChangeTaskStatusAsync(Guid projectId, Guid taskId, ChangeTaskStatusRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken);
    }
}
