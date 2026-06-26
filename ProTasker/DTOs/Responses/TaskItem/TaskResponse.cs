namespace ProTasker.DTOs.Responses.TaskItem
{
    public record TaskResponse(
        Guid Id, Guid ProjectId, Guid? UserId, string Title,
        string? Description, TaskStatus Status, DateTime CreatedAt, 
        DateTime? DueDate);
}
