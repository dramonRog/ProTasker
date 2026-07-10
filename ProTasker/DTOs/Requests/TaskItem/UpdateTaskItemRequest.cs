using ProTasker.Models;

namespace ProTasker.DTOs.Requests.TaskItem
{
    public record UpdateTaskItemRequest(string? Title, string? Description, TaskPriority? Priority, DateTime? DueDate);
}
