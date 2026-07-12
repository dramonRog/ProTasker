using ProTasker.Models.Enums;

namespace ProTasker.DTOs.Requests.TaskItem
{
    public record GetTasksQueryParameters(TaskPriority? Priority, Guid? AssigneeId, string? SearchTerm);
}
