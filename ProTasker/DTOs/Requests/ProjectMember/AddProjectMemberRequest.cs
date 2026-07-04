using ProTasker.Models;

namespace ProTasker.DTOs.Requests.ProjectMember
{
    public record AddProjectMemberRequest(Guid UserId, ProjectRole Role = ProjectRole.Member);
}
