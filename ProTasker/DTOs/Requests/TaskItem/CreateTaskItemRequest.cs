using ProTasker.Models;

namespace ProTasker.DTOs.Requests.TaskItem
{
    public record CreateTaskItemRequest(
        string Title, 
        string? Description,
        DateTime? DueDate,
        Guid ProjectId,
        TaskPriority Priority,
        Guid? AssignedUserId = null,
        Guid? BoardId = null
    );
}
