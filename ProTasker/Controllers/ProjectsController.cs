using Microsoft.AspNetCore.Mvc;
using ProTasker.DTOs.Requests.Project;
using ProTasker.DTOs.Responses.Project;
using ProTasker.Services;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<List<ProjectListItemResponse>> GetAllProjects(CancellationToken cancellationToken)
        {
            return await _projectService.GetAllAsync(cancellationToken);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProjectDetailsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProjectDetailsResponse>> GetProjectById(Guid id, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetByIdAsync(id, cancellationToken);

            if (project == null)
                return NotFound("The project does not exist.");

            return Ok(project);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProjectDetailsResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ProjectDetailsResponse>> CreateProject(CreateProjectRequest request, CancellationToken cancellationToken)
        {
            var project = await _projectService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProjectDetailsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProjectDetailsResponse>> UpdateProject(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
        {
            var project = await _projectService.UpdateAsync(id, request, cancellationToken);

            if (project == null)
                return NotFound();

            return Ok(project);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
        {
            bool result = await _projectService.DeleteAsync(id, cancellationToken);

            return result ? NoContent() : NotFound();
        }
    }
}
