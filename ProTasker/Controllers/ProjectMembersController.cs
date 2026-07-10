using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProTasker.Common;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.DTOs.Responses.ProjectMember;
using ProTasker.Services.Interfaces;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectMembersController : ControllerBase
    {
        private readonly IProjectMemberService _projectMemberService;

        public ProjectMembersController(IProjectMemberService projectMemberService)
        {
            _projectMemberService = projectMemberService;
        }

        [HttpGet("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(List<ProjectMemberResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProjectMemberResponse>>> GetAllMembers(Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _projectMemberService.GetAllAsync(projectId, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpGet("{userId:guid}/{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProjectMemberResponse>> GetMemberById(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _projectMemberService.GetByIdAsync(userId, projectId, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPost("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ProjectMemberResponse>> CreateMember(Guid projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
            var result = await _projectMemberService.AddProjectMemberToProjectAsync(projectId, request, cancellationToken);

            if (!result.IsSuccess)
                return result.CastToResultCode();

            return CreatedAtAction(nameof(GetMemberById), new { userId = request.UserId, projectId = projectId }, result.Value);
        }

        [HttpPatch("{userId:guid}/{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProjectMemberResponse>> ChangeProjectMemberRole(Guid userId, Guid projectId, ChangeProjectMemberRole request, CancellationToken cancellationToken)
        {
            var result = await _projectMemberService.ChangeProjectMemberRoleAsync(userId, projectId, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpDelete("{userId:guid}/{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteMemberById(Guid userId, Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _projectMemberService.DeleteByIdAsync(userId, projectId, cancellationToken);
            return result.CastToResultCode();
        }
    }
}
