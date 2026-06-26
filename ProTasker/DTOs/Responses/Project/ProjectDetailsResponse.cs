using ProTasker.DTOs.Responses.ProjectMember;

namespace ProTasker.DTOs.Responses.Project
{
    public record ProjectDetailsResponse(Guid Id, string Name, string? Description, DateTime CreatedAt, List<ProjectMemberResponse> Members);
}
