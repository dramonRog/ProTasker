using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.DTOs.Responses.ProjectMember;
using ProTasker.Services;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectMembersController : ControllerBase
    {
        private readonly IProjectMemberService _projectMemberService;

        public ProjectMembersController(IProjectMemberService projectMemberService)
        {
            _projectMemberService = projectMemberService;
        }

        [HttpGet]
        public async Task<List<ProjectMemberResponse>> GetAllMembers(CancellationToken cancellationToken)
        {
            return await _projectMemberService.GetAllAsync(cancellationToken);
        }

        [HttpGet("{userId:guid},{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProjectMemberResponse>> GetMemberById(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            ProjectMemberResponse? projectMember = await _projectMemberService.GetByIdAsync(userId, projectId, cancellationToken);

            if (projectMember == null)
                return NotFound();

            return Ok(projectMember);
        }

        [HttpPost("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ProjectMemberResponse>> CreateMember(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
            ProjectMemberResponse? projectMember = await _projectMemberService.AddProjectMemberToProjectAsync(projectId, request, cancellationToken);

            if (projectMember == null)
                return NotFound();

            return CreatedAtAction(nameof(GetMemberById), new { userId = request.UserId, projectId = projectId }, projectMember);
        }

        [HttpDelete("{userId:guid},{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteMemberById(Guid userId, Guid projectId, CancellationToken cancellationtoken)
        {
            bool result = await _projectMemberService.DeleteByIdAsync(userId, projectId, cancellationtoken);

            return result ? NoContent() : NotFound();
        }
    }
}
