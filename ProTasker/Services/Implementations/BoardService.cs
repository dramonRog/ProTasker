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
        private readonly IMapper _mapper;
        private readonly ILogger<BoardService> _logger;

        public BoardService(AppDbContext context, IUserContextService userContextService, IMapper mapper, ILogger<BoardService> logger)
        {
            _context = context;
            _userContextService = userContextService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<BoardResponse>>> GetAllByProjectAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Result access = await EnsureMemberAsync(projectId, cancellationToken);
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

            Result access = await EnsureMemberAsync(board.ProjectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<BoardResponse>.Forbidden(access.Error);

            return Result<BoardResponse>.Success(_mapper.Map<BoardResponse>(board));
        }

        public async Task<Result<BoardResponse>> CreateAsync(Guid projectId, CreateBoardRequest request, CancellationToken cancellationToken)
        {
            Result access = await EnsureAdminAsync(projectId, cancellationToken);
            if (!access.IsSuccess)
                return Result<BoardResponse>.Forbidden(access.Error);

            if (!await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
                return Result<BoardResponse>.NotFound("Project was not found.");

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

            Result access = await EnsureAdminAsync(board.ProjectId, cancellationToken);
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

        public async Task<Result> DeleteAsync(Guid boardId, CancellationToken cancellationToken)
        {
            Board? board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);
            if (board == null)
                return Result.NotFound("Board was not found.");

            Result access = await EnsureAdminAsync(board.ProjectId, cancellationToken);
            if (!access.IsSuccess)
                return Result.Forbidden(access.Error);

            bool hasTasks = await _context.TaskItems.AnyAsync(t => t.BoardId == boardId, cancellationToken);
            if (hasTasks)
                return Result.Conflict("Cannot delete a board that contains tasks.");

            bool isLastBoard = !await _context.Boards
                .AnyAsync(b => b.ProjectId == board.ProjectId && b.Id != boardId, cancellationToken);

            if (isLastBoard)
                return Result.Conflict("Cannot delete the last board in a project.");

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Board {BoardId} deleted from project {ProjectId}.", boardId, board.ProjectId);

            return Result.Success();
        }

        private async Task<Result> EnsureMemberAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            bool isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (!isMember)
            {
                _logger.LogWarning("User {UserId} attempted to access project {ProjectId} without being a member.", currentId, projectId);
                return Result.Forbidden("You are not a member of this project.");
            }

            return Result.Success();
        }

        private async Task<Result> EnsureAdminAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (member == null)
                return Result.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to manage boards in project {ProjectId} without Admin role.", currentId, projectId);
                return Result.Forbidden("Only administrators can manage boards.");
            }

            return Result.Success();
        }
    }
}