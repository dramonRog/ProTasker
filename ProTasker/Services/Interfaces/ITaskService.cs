using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;
using ProTasker.Pagination;

namespace ProTasker.Services.Interfaces
{
    public interface ITaskService
    {
        Task<Result<PagedResult<TaskResponse>>> GetAllProjectTasksAsync(Guid projectId, PaginationQuery pagination, GetTasksQueryParameters queryParameters, CancellationToken cancellationToken);
        Task<Result<PagedResult<TaskResponse>>> GetAllUserTasksAsync(Guid? projectId, Guid userId, PaginationQuery pagination, GetTasksQueryParameters queryParameters, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> UpdateAsync(Guid taskId, UpdateTaskItemRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> AssignTaskAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> MoveTaskToBoardAsync(Guid taskId, MoveTaskToBoardRequest request, CancellationToken cancellationToken);
        Task<Result<TaskResponse>> ChangeTaskPriorityAsync(Guid taskId, ChangeTaskPriorityRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteByIdAsync(Guid taskId, CancellationToken cancellationToken);
    }
}
