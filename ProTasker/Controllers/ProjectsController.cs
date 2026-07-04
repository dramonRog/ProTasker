using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProTasker.Common;
using ProTasker.DTOs.Requests.Project;
using ProTasker.DTOs.Responses.Project;
using ProTasker.Services;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ProjectListItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProjectListItemResponse>>> GetAllProjects(CancellationToken cancellationToken)
        {
            var projectItems = await _projectService.GetAllAsync(cancellationToken);
            return projectItems.CastToResultCode();
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProjectDetailsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProjectDetailsResponse>> GetProjectById(Guid id, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetByIdAsync(id, cancellationToken);

            return project.CastToResultCode();
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProjectDetailsResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProjectDetailsResponse>> CreateProject(CreateProjectRequest request, CancellationToken cancellationToken)
        {
            var projectResult = await _projectService.CreateAsync(request, cancellationToken);

            if (!projectResult.IsSuccess)
                return projectResult.CastToResultCode();

            return CreatedAtAction(nameof(GetProjectById), new { id = projectResult.Value!.Id }, projectResult.Value);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProjectDetailsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProjectDetailsResponse>> UpdateProject(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
        {
            var projectResult = await _projectService.UpdateAsync(id, request, cancellationToken);
            return projectResult.CastToResultCode();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
        {
            var projectResult = await _projectService.DeleteAsync(id, cancellationToken);
            return projectResult.CastToResultCode();
        }
    }
}
