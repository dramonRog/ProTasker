using ProTasker.Models;

namespace ProTasker.DTOs.Requests.TaskItem
{
    public record ChangeTaskPriorityRequest(TaskPriority Priority);
}
