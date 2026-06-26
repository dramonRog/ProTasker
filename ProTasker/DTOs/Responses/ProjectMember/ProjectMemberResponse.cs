namespace ProTasker.DTOs.Responses.ProjectMember
{
    public record ProjectMemberResponse(Guid ProjectId, Guid UserId, string FirstName, string LastName, string Email, DateTime AddedAt);
}
