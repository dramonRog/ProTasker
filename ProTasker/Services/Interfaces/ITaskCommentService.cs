using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskComment;
using ProTasker.DTOs.Responses.TaskComment;
using ProTasker.Pagination;

namespace ProTasker.Services.Interfaces
{
    public interface ITaskCommentService
    {
        Task<Result<PagedResult<TaskCommentResponse>>> GetTaskCommentsAsync(Guid taskId, PaginationQuery pagination, CancellationToken cancellationToken);
        Task<Result<TaskCommentResponse>> GetTaskCommentByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<Result<TaskCommentResponse>> CreateTaskCommentAsync(Guid taskId, CreateTaskCommentRequest request, CancellationToken cancellationToken);
        Task<Result<TaskCommentResponse>> UpdateTaskCommentAsync(Guid id, UpdateTaskCommentRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteCommentAsync(Guid id, CancellationToken cancellationToken);
    }
}
