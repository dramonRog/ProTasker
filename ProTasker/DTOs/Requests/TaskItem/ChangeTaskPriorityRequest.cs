using ProTasker.Models.Enums;

namespace ProTasker.DTOs.Requests.TaskItem
{
    public record ChangeTaskPriorityRequest(TaskPriority Priority);
}
