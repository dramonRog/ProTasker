namespace ProTasker.DTOs.Responses.TaskComment
{
    public record TaskCommentResponse(Guid Id, Guid TaskId, Guid? UserId, string? AuthorName, string Title, string Description, DateTime CreatedAt);
}
