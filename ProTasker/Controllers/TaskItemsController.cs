using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;
using ProTasker.Services;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskItemsController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskItemsController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaskResponse>>> GetAllProjectTasks(Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _taskService.GetAllProjectTasksAsync(projectId, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaskResponse>>> GetAllUserTasks(Guid userId, [FromQuery] Guid? projectId, CancellationToken cancellationToken)
        {
            var result = await _taskService.GetAllUserTasksAsync(projectId, userId, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpGet("project/{projectId:guid}/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> GetTaskById(Guid projectId, Guid taskId, CancellationToken cancellationToken)
        {
            var result = await _taskService.GetByIdAsync(projectId, taskId, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.CreateAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return result.CastToResultCode();

            return CreatedAtAction(nameof(GetTaskById), new { projectId = request.ProjectId, taskId = result.Value!.Id }, result.Value);
        }

        [HttpPut("{projectId:guid}/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> UpdateTask(Guid projectId, Guid taskId, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.UpdateAsync(projectId, taskId, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPatch("{projectId:guid}/{taskId:guid}/status")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> ChangeTaskStatus(Guid projectId, Guid taskId, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.ChangeTaskStatusAsync(projectId, taskId, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPatch("{projectId:guid}/{taskId:guid}/assignee")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> AssignTask(Guid projectId, Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.AssignTaskAsync(projectId, taskId, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpDelete("{projectId:guid}/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteTaskById(Guid projectId, Guid taskId, CancellationToken cancellationToken)
        {
            var result = await _taskService.DeleteByIdAsync(projectId, taskId, cancellationToken);
            return result.CastToResultCode();
        }
    }
}
