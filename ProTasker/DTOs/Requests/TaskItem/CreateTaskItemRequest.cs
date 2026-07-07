using ProTasker.Models;

namespace ProTasker.DTOs.Requests.TaskItem
{
    public record CreateTaskItemRequest(
        string Title, 
        string? Description,
        DateTime? DueDate,
        Guid ProjectId,
        Guid? AssignedUserId = null,
        ProTasker.Models.TaskStatus Status = Models.TaskStatus.ToDo
    );
}
