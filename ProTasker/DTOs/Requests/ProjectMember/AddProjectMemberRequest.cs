using ProTasker.Models.Enums;

namespace ProTasker.DTOs.Requests.ProjectMember
{
    public record AddProjectMemberRequest(Guid UserId, ProjectRole Role = ProjectRole.Member);
}
