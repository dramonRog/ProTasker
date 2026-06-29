using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<List<TaskResponse>> GetAllTasks(CancellationToken cancellationToken)
        {
            return await _taskService.GetAllAsync(cancellationToken);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> GetTaskById(Guid id, CancellationToken cancellationToken)
        {
            TaskResponse? task = await _taskService.GetByIdAsync(id, cancellationToken);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            TaskResponse task = await _taskService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> UpdateTask(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            TaskResponse? task = await _taskService.UpdateAsync(id, request, cancellationToken);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> ChangeTaskStatus(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            TaskResponse? task = await _taskService.ChangeTaskStatusAsync(id, request, cancellationToken);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpPatch("{id:guid}/assignee")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskResponse>> AssignTask(Guid id, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            TaskResponse? task = await _taskService.AssignTaskAsync(id, request, cancellationToken);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteTaskById(Guid id, CancellationToken cancellationToken)
        {
            bool result = await _taskService.DeleteByIdAsync(id, cancellationToken);

            return result ? NoContent() : NotFound();
        }
    }
}
