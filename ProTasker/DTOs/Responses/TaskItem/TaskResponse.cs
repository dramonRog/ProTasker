using ProTasker.DTOs.Responses.Board;

namespace ProTasker.DTOs.Responses.TaskItem
{
    public record TaskResponse(
        Guid Id, Guid ProjectId, Guid? UserId, string Title,
        string? Description, DateTime CreatedAt, 
        DateTime? DueDate, BoardSummaryResponse? Board);
}
