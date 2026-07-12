using ProTasker.Models.Enums;

namespace ProTasker.DTOs.Requests.TaskItem
{
    public record UpdateTaskItemRequest(string? Title, string? Description, TaskPriority? Priority, DateTime? DueDate);
}
