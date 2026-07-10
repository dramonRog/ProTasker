using ProTasker.Common;
using ProTasker.DTOs.Requests.Board;
using ProTasker.DTOs.Responses.Board;
using ProTasker.Models;

namespace ProTasker.Services.Interfaces
{
    public interface IBoardService
    {
        Task<Result<List<BoardResponse>>> GetAllByProjectAsync(Guid projectId, CancellationToken cancellationToken);
        Task<Result<BoardResponse>> GetByIdAsync(Guid boardId, CancellationToken cancellationToken);
        Task<Result<BoardResponse>> CreateAsync(Guid projectId, CreateBoardRequest request, CancellationToken cancellationToken);
        Task<Result<BoardResponse>> UpdateAsync(Guid boardId, UpdateBoardRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteAsync(Guid boardId, CancellationToken cancellationToken);
    }
}
