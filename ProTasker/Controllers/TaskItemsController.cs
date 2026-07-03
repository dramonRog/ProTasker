using Microsoft.AspNetCore.Mvc;
using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;
using ProTasker.Services;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskItemsController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskItemsController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaskResponse>>> GetAllTasks(CancellationToken cancellationToken)
        {
            var result = await _taskService.GetAllAsync(cancellationToken);
            return result.CastToResultCode();
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> GetTaskById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _taskService.GetByIdAsync(id, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPost]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.CreateAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return result.CastToResultCode();

            return CreatedAtAction(nameof(GetTaskById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskResponse>> UpdateTask(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.UpdateAsync(id, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> ChangeTaskStatus(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.ChangeTaskStatusAsync(id, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPatch("{id:guid}/assignee")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> AssignTask(Guid id, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskService.AssignTaskAsync(id, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteTaskById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _taskService.DeleteByIdAsync(id, cancellationToken);
            return result.CastToResultCode();
        }
    }
}
