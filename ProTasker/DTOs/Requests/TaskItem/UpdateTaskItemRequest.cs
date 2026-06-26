namespace ProTasker.DTOs.Requests.TaskItem
{
    public record UpdateTaskItemRequest(string? Title, string? Description, DateTime? DueDate);
}
