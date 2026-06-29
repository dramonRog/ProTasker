using ProTasker.Models;

namespace ProTasker.DTOs.Responses.TaskItem
{
    public record TaskResponse(
        Guid Id, Guid ProjectId, Guid? UserId, string Title,
        string? Description, ProTasker.Models.TaskStatus Status, DateTime CreatedAt, 
        DateTime? DueDate);
}
