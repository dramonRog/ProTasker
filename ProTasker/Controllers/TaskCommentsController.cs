using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProTasker.Common;
using ProTasker.DTOs.Requests.TaskComment;
using ProTasker.DTOs.Responses.TaskComment;
using ProTasker.Pagination;
using ProTasker.Services.Interfaces;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskCommentsController : ControllerBase
    {
        private readonly ITaskCommentService _taskCommentService;

        public TaskCommentsController(ITaskCommentService taskCommentService)
        {
            _taskCommentService = taskCommentService;
        }

        [HttpGet("task/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(PagedResult<TaskCommentResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<TaskCommentResponse>>> GetTaskComments(Guid taskId, [FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
        {
            var result = await _taskCommentService.GetTaskCommentsAsync(taskId, pagination, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TaskCommentResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskCommentResponse>> GetTaskCommentById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _taskCommentService.GetTaskCommentByIdAsync(id, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPost("task/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TaskCommentResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<TaskCommentResponse>> CreateTaskComment(Guid taskId, CreateTaskCommentRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskCommentService.CreateTaskCommentAsync(taskId, request, cancellationToken);
            
            if (!result.IsSuccess)
                return result.CastToResultCode();
            return CreatedAtAction(nameof(GetTaskCommentById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TaskCommentResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskCommentResponse>> UpdateTaskComment(Guid id, UpdateTaskCommentRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskCommentService.UpdateTaskCommentAsync(id, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteTaskComment(Guid id, CancellationToken cancellationToken)
        {
            var result = await _taskCommentService.DeleteCommentAsync(id, cancellationToken);
            return result.CastToResultCode();
        }
    }
}
