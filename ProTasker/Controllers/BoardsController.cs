using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProTasker.Common;
using ProTasker.DTOs.Requests.Board;
using ProTasker.DTOs.Responses.Board;
using ProTasker.Services.Interfaces;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BoardsController : ControllerBase
    {
        private readonly IBoardService _boardService;

        public BoardsController(IBoardService boardService)
        {
            _boardService = boardService;
        }

        [HttpGet("project/{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(List<BoardResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<BoardResponse>>> GetAllByProject(Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _boardService.GetAllByProjectAsync(projectId, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpGet("{boardId:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<BoardResponse>> GetById(Guid boardId, CancellationToken cancellationToken)
        {
            var result = await _boardService.GetByIdAsync(boardId, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPost("project/{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<BoardResponse>> CreateBoard(Guid projectId, CreateBoardRequest request, CancellationToken cancellationToken)
        {
            var result = await _boardService.CreateAsync(projectId, request, cancellationToken);

            if (!result.IsSuccess)
                return result.CastToResultCode();

            return CreatedAtAction(nameof(GetById), new { boardId = result.Value!.Id }, result.Value);
        }

        [HttpPut("{boardId:guid}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<BoardResponse>> UpdateBoard(Guid boardId, UpdateBoardRequest request, CancellationToken cancellationToken)
        {
            var result = await _boardService.UpdateAsync(boardId, request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPut("project/{projectId:guid}/reorder")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ReorderBoards(Guid projectId, ReorderBoardsRequest request, CancellationToken cancellationToken)
        {
            var result = await _boardService.ReorderAsync(projectId, request, cancellationToken);
            return result.CastToResultCode();
        }


        [HttpDelete("{boardId:guid}")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteBoard(Guid boardId, CancellationToken cancellationToken)
        {
            var result = await _boardService.DeleteAsync(boardId, cancellationToken);
            return result.CastToResultCode();
        }
    }
}