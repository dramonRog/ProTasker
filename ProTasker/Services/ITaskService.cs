using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;

namespace ProTasker.Services
{
    public interface ITaskService
    {
        Task<Result<List<TaskResponse>>> GetAllAsync(CancellationToken cancellationToken);
        Task<Result<TaskResponse>> GetByIdAsync(Guid Id, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> UpdateAsync(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> ChangeTaskStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteByIdAsync(Guid Id, CancellationToken cancellationToken);
    }
}
