using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.Board;
using ProTasker.DTOs.Responses.Board;
using ProTasker.Models;
using ProTasker.Services.Interfaces;

namespace ProTasker.Services.Implementations
{
    public class BoardService : IBoardService
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly IProjectAccessService _projectAccessService;
        private readonly IMapper _mapper;
        private readonly ILogger<BoardService> _logger;

        public BoardService(AppDbContext context, IUserContextService userContextService, IProjectAccessService projectAccessService, IMapper mapper, ILogger<BoardService> logger)
        {
            _context = context;
            _userContextService = userContextService;
            _projectAccessService = projectAccessService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<BoardResponse>>> GetAllByProjectAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Result access = await _projectAccessService.EnsureMemberAsync(projectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<List<BoardResponse>>.Forbidden(access.Error);

            List<BoardResponse> boards = await _context.Boards
                .AsNoTracking()
                .Where(b => b.ProjectId == projectId)
                .OrderBy(b => b.OrderIndex)
                .ProjectTo<BoardResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<BoardResponse>>.Success(boards);
        }

        public async Task<Result<BoardResponse>> GetByIdAsync(Guid boardId, CancellationToken cancellationToken)
        {
            Board? board = await _context.Boards
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

            if (board == null)
                return Result<BoardResponse>.NotFound("Board was not found.");

            Result access = await _projectAccessService.EnsureMemberAsync(board.ProjectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<BoardResponse>.Forbidden(access.Error);

            return Result<BoardResponse>.Success(_mapper.Map<BoardResponse>(board));
        }

        public async Task<Result<BoardResponse>> CreateAsync(Guid projectId, CreateBoardRequest request, CancellationToken cancellationToken)
        {
            Result access = await _projectAccessService.EnsureAdminAsync(projectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<BoardResponse>.Forbidden(access.Error);

            bool orderTaken = await _context.Boards
                .AnyAsync(b => b.ProjectId == projectId && b.OrderIndex == request.OrderIndex, cancellationToken);

            if (orderTaken)
                return Result<BoardResponse>.Conflict("A board with this order index already exists in the project.");

            Board board = new Board
            {
                Name = request.Name,
                OrderIndex = request.OrderIndex,
                Color = request.Color,
                ProjectId = projectId
            };

            _context.Boards.Add(board);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Board {BoardId} created in project {ProjectId}.", board.Id, projectId);

            return Result<BoardResponse>.Success(_mapper.Map<BoardResponse>(board));
        }

        public async Task<Result<BoardResponse>> UpdateAsync(Guid boardId, UpdateBoardRequest request, CancellationToken cancellationToken)
        {
            Board? board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);
            if (board == null)
                return Result<BoardResponse>.NotFound("Board was not found.");

            Result access = await _projectAccessService.EnsureAdminAsync(board.ProjectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<BoardResponse>.Forbidden(access.Error);

            if (request.OrderIndex.HasValue && request.OrderIndex.Value != board.OrderIndex)
            {
                bool orderTaken = await _context.Boards
                    .AnyAsync(b => b.ProjectId == board.ProjectId
                        && b.OrderIndex == request.OrderIndex.Value
                        && b.Id != boardId, cancellationToken);

                if (orderTaken)
                    return Result<BoardResponse>.Conflict("A board with this order index already exists in the project.");
            }

            if (request.Name is not null)
                board.Name = request.Name;

            if (request.OrderIndex.HasValue)
                board.OrderIndex = request.OrderIndex.Value;

            if (request.Color is not null)
                board.Color = request.Color;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Board {BoardId} updated.", boardId);

            return Result<BoardResponse>.Success(_mapper.Map<BoardResponse>(board));
        }

        public async Task<Result> ReorderAsync(Guid projectId, ReorderBoardsRequest request, CancellationToken cancellationToken)
        {
            Result adminResult = await _projectAccessService.EnsureAdminAsync(projectId, cancellationToken);
            if (!adminResult.IsSuccess)
                return Result.Forbidden(adminResult.Error);

            List<Board> boards = await _context.Boards
                .Where(b => b.ProjectId == projectId)
                .ToListAsync(cancellationToken);

            if (boards.Count != request.BoardIds.Count || !request.BoardIds.All(id => boards.Any(b => b.Id == id)))
                return Result.Validation("The provided board IDs are invalid or do not match the existing boards in the project.");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var board in boards)
                {
                    board.OrderIndex = -Math.Abs(board.OrderIndex) - 1000;
                }

                await _context.SaveChangesAsync(cancellationToken);

                Dictionary<Guid, Board> boardsDict = boards.ToDictionary(b => b.Id);
                for (int i = 0; i < request.BoardIds.Count; i++)
                {
                    var board = boardsDict[request.BoardIds[i]];
                    board.OrderIndex = i;
                }
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            Guid currentId = _userContextService.GetCurrentUserId();
            _logger.LogInformation("Admin {UserId} successfully reordered boards in project {ProjectId}.", currentId, projectId);

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid boardId, CancellationToken cancellationToken)
        {
            Board? board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);
            if (board == null)
                return Result.NotFound("Board was not found.");

            Result access = await _projectAccessService.EnsureAdminAsync(board.ProjectId, cancellationToken);
            if (!access.IsSuccess)
                return Result.Forbidden(access.Error);

            bool isLastBoard = !await _context.Boards
                .AnyAsync(b => b.ProjectId == board.ProjectId && b.Id != boardId, cancellationToken);

            if (isLastBoard)
                return Result.Conflict("Cannot delete the last board in a project.");

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Board {BoardId} deleted from project {ProjectId}.", boardId, board.ProjectId);

            return Result.Success();
        }
    }
}