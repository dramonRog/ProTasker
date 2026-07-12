using ProTasker.DTOs.Responses.Board;
using ProTasker.Models.Enums;

namespace ProTasker.DTOs.Responses.TaskItem
{
    public record TaskResponse(
        Guid Id, Guid ProjectId, Guid? UserId, string Title,
        string? Description, TaskPriority Priority, DateTime CreatedAt, 
        DateTime? DueDate, BoardSummaryResponse? Board);
}
