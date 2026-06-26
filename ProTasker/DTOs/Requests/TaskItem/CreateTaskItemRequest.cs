namespace ProTasker.DTOs.Requests.TaskItem
{
    public record CreateTaskItemRequest(
        string Title, string? Description,
        DateTime? DueDate, Guid ProjectId
    );
}
