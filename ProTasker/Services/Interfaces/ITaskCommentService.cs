using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskComment;
using ProTasker.DTOs.Responses.TaskComment;

namespace ProTasker.Services.Interfaces
{
    public interface ITaskCommentService
    {
        Task<Result<List<TaskCommentResponse>>> GetTaskCommentsAsync(Guid taskId, CancellationToken cancellationToken);
        Task<Result<TaskCommentResponse>> GetTaskCommentByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<Result<TaskCommentResponse>> CreateTaskCommentAsync(Guid taskId, CreateTaskCommentRequest request, CancellationToken cancellationToken);
        Task<Result<TaskCommentResponse>> UpdateTaskCommentAsync(Guid id, UpdateTaskCommentRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteCommentAsync(Guid id, CancellationToken cancellationToken);
    }
}
