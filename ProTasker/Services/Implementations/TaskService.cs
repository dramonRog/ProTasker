using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;
using ProTasker.Models;
using ProTasker.Models.Enums;
using ProTasker.Pagination;
using ProTasker.Services.Interfaces;

namespace ProTasker.Services.Implementations
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly IProjectAccessService _projectAccessService;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;

        public TaskService(AppDbContext context, IUserContextService userContextService, IProjectAccessService projectAccessService, IMapper mapper, ILogger<TaskService> logger)
        {
            _context = context;
            _userContextService = userContextService;
            _projectAccessService = projectAccessService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedResult<TaskResponse>>> GetAllProjectTasksAsync(Guid projectId, PaginationQuery pagination, GetTasksQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            Result result = await _projectAccessService.EnsureMemberAsync(projectId, cancellationToken);

            if (!result.IsSuccess)
                return Result<PagedResult<TaskResponse>>.Forbidden(result.Error);

            IQueryable<TaskItem> query = _context.TaskItems
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId);

            if (queryParameters.AssigneeId != null)
                query = query.Where(t => t.UserId == queryParameters.AssigneeId);
            if (queryParameters.Priority != null)
                query = query.Where(t => t.Priority == queryParameters.Priority);
            if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
            {
                string searchPattern = $"%{queryParameters.SearchTerm}%";
                query = query.Where(t => EF.Functions.ILike(t.Title, searchPattern) ||
                    (t.Description != null && EF.Functions.ILike(t.Description, searchPattern)));
            }

            int totalCount = await query.CountAsync(cancellationToken);
            int skipAmount = (pagination.PageNumber - 1) * pagination.PageSize;

            List<TaskResponse> taskItems = await query
                .OrderBy(t => t.CreatedAt)
                .Skip(skipAmount)
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<PagedResult<TaskResponse>>.Success(new PagedResult<TaskResponse>(taskItems, totalCount, pagination.PageNumber, pagination.PageSize));
        }

        public async Task<Result<PagedResult<TaskResponse>>> GetAllUserTasksAsync(Guid? projectId, Guid userId, PaginationQuery pagination, GetTasksQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            if (currentId != userId && projectId == null)
            {
                _logger.LogWarning("User {UserId} attempted to view all tasks of user {TargetUserId} without specifying a shared project context.", currentId, userId);
                return Result<PagedResult<TaskResponse>>.Forbidden("You can't get tasks of this user");
            }

            if (projectId != null)
            {
                Result result = await _projectAccessService.EnsureMemberAsync(projectId.Value, cancellationToken);
                if (!result.IsSuccess)
                    return Result<PagedResult<TaskResponse>>.Forbidden(result.Error);

                if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken))
                    return Result<PagedResult<TaskResponse>>.NotFound("User was not found as a member of this project.");
            }

            IQueryable<Guid> visibleProjectIds = _context.Projects
                .Where(p => p.ProjectMembers.Any(pm => pm.UserId == currentId))
                .Select(p => p.Id);
            
            IQueryable<TaskItem> query = _context.TaskItems
                .AsNoTracking()
                .Where(t => t.UserId == userId && visibleProjectIds.Contains(t.ProjectId));
            
            if (projectId != null)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (queryParameters.Priority != null)
                query = query.Where(t => t.Priority == queryParameters.Priority);
            if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
            {
                string searchPattern = $"%{queryParameters.SearchTerm}%";
                query = query.Where(t => EF.Functions.ILike(t.Title, searchPattern) ||
                    (t.Description != null && EF.Functions.ILike(t.Description, searchPattern)));
            }

            int totalCount = await query.CountAsync(cancellationToken);
            int skipAmount = (pagination.PageNumber - 1) * pagination.PageSize;

            List<TaskResponse> taskItems = await query
                .OrderBy(t => t.CreatedAt)
                .Skip(skipAmount)
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<PagedResult<TaskResponse>>.Success(new PagedResult<TaskResponse>(taskItems, totalCount, pagination.PageNumber, pagination.PageSize));
        }

        public async Task<Result<TaskResponse>> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
        {
            Result<TaskItem> taskResult = await GetTaskIfMemberAsync(taskId, trackChanges: false, cancellationToken);

            if (!taskResult.IsSuccess)
                return Result<TaskResponse>.NotFound(taskResult.Error);

            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(taskResult.Value!));
        }

        public async Task<Result<TaskResponse>> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            Result<ProjectMember> adminResult = await _projectAccessService.EnsureAdminAsync(request.ProjectId, cancellationToken);
            if (!adminResult.IsSuccess)
                return Result<TaskResponse>.Forbidden(adminResult.Error);

            Guid currentId = _userContextService.GetCurrentUserId();

            if (request.AssignedUserId.HasValue)
            {
                bool isAssigneeMember = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.AssignedUserId.Value, cancellationToken);

                if (!isAssigneeMember)
                {
                    _logger.LogWarning("Admin {UserId} attempted to assign a new task to non-member {TargetUserId} in project {ProjectId}.", currentId, request.AssignedUserId.Value, request.ProjectId);
                    return Result<TaskResponse>.NotFound("Assigned user is not a member of this project.");
                }
            }

            Result<Board> boardResult = await ResolveBoardForProjectAsync(request.ProjectId, request.BoardId, cancellationToken);

            if (!boardResult.IsSuccess)
                return Result<TaskResponse>.NotFound(boardResult.Error);

            TaskItem task = _mapper.Map<TaskItem>(request);
            task.UserId = request.AssignedUserId;
            task.BoardId = boardResult.Value!.Id;
            task.Board = boardResult.Value;

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {UserId} successfully created task {TaskId} in project {ProjectId}.", currentId, task.Id, request.ProjectId);

            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(task));
        }

        public async Task<Result<TaskResponse>> UpdateAsync(Guid taskId, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            Result<TaskItem> taskResult = await GetTaskIfMemberAsync(taskId, trackChanges: true, cancellationToken);
            if (!taskResult.IsSuccess)
                return Result<TaskResponse>.NotFound(taskResult.Error);

            Result modifyAccess = await EnsureCanModifyTaskAsync(taskResult.Value!, cancellationToken);
            if (!modifyAccess.IsSuccess)
                return Result<TaskResponse>.Forbidden(modifyAccess.Error);

            TaskItem task = taskResult.Value!;

            if (request.Title is not null)
                task.Title = request.Title;

            if (request.Description is not null)
                task.Description = request.Description;

            if (request.DueDate is not null)
                task.DueDate = request.DueDate;

            if (request.Priority is not null)
                task.Priority = request.Priority.Value;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {UserId} successfully updated data for task {TaskId}.", currentId, taskId);
            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(task));
        }

        public async Task<Result<TaskResponse>> AssignTaskAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            Result<TaskItem> taskResult = await GetTaskIfMemberAsync(taskId, trackChanges: true, cancellationToken);
            if (!taskResult.IsSuccess)
                return Result<TaskResponse>.NotFound(taskResult.Error);

            Result<ProjectMember> adminResult = await _projectAccessService.EnsureAdminAsync(taskResult.Value!.ProjectId, cancellationToken);
            bool isAdmin = adminResult.IsSuccess;

            if (!isAdmin)
            {
                if (request.UserId.HasValue && request.UserId.Value != currentId)
                {
                    _logger.LogWarning("User {UserId} attempted to assign task {TaskId} to another user {TargetUserId}.", currentId, taskId, request.UserId);
                    return Result<TaskResponse>.Forbidden("You can only assign tasks to yourself.");
                }

                if (!request.UserId.HasValue && taskResult.Value!.UserId != currentId)
                {
                    _logger.LogWarning("User {UserId} attempted to unassign task {TaskId} which is assigned to someone else.", currentId, taskId);
                    return Result<TaskResponse>.Forbidden("You can only unassign your own tasks.");
                }
            }

            if (request.UserId.HasValue && request.UserId.Value == currentId && taskResult.Value!.UserId.HasValue && taskResult.Value!.UserId != currentId)
            {
                _logger.LogWarning("User {UserId} attempted to steal task {TaskId} from user {TargetUserId}.", currentId, taskId, taskResult.Value!.UserId);
                return Result<TaskResponse>.Forbidden("This task is already assigned to another user.");
            }

            taskResult.Value!.UserId = request.UserId;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully assigned task {TaskId} to user {TargetUserId}.", currentId, taskId, request.UserId);
            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(taskResult.Value));
        }

        public async Task<Result<TaskResponse>> MoveTaskToBoardAsync(Guid taskId, MoveTaskToBoardRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            Result<TaskItem> taskResult = await GetTaskIfMemberAsync(taskId, trackChanges: true, cancellationToken);
            if (!taskResult.IsSuccess)
                return Result<TaskResponse>.NotFound(taskResult.Error);

            Result modifyAccess = await EnsureCanModifyTaskAsync(taskResult.Value!, cancellationToken);
            if (!modifyAccess.IsSuccess)
                return Result<TaskResponse>.Forbidden(modifyAccess.Error);

            Board? board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId && b.ProjectId == taskResult.Value!.ProjectId, cancellationToken);

            if (board == null)
                return Result<TaskResponse>.NotFound("Board was not found in this project.");

            taskResult.Value!.BoardId = board.Id;
            taskResult.Value.Board = board;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully changed status of task {TaskId} to board {BoardId} ('{BoardName}').", currentId, taskId, board.Id, board.Name);
            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(taskResult.Value));
        }

        public async Task<Result<TaskResponse>> ChangeTaskPriorityAsync(Guid taskId, ChangeTaskPriorityRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            Result<TaskItem> taskResult = await GetTaskIfMemberAsync(taskId, trackChanges: true, cancellationToken);
            if (!taskResult.IsSuccess)
                return Result<TaskResponse>.NotFound(taskResult.Error);

            Result userResult = await EnsureCanModifyTaskAsync(taskResult.Value!, cancellationToken);
            if (!userResult.IsSuccess)
                return Result<TaskResponse>.Forbidden(userResult.Error);

            taskResult.Value!.Priority = request.Priority;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully changed the priority of task {TaskId} into {Priority}.", currentId, taskResult.Value.Id, request.Priority);
            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(taskResult.Value));
        }

        public async Task<Result> DeleteByIdAsync(Guid taskId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            Result<TaskItem> taskResult = await GetTaskIfMemberAsync(taskId, trackChanges: true, cancellationToken);
            if (!taskResult.IsSuccess)
                return Result.NotFound(taskResult.Error);

            Result<ProjectMember> adminResult = await _projectAccessService.EnsureAdminAsync(taskResult.Value!.ProjectId, cancellationToken);
            if (!adminResult.IsSuccess)
                return Result.Forbidden(adminResult.Error);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.TaskItems.Remove(taskResult.Value);

                await _context.TaskComments
                    .Where(tc => tc.TaskId == taskId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(tc => tc.IsDeleted, true)
                        .SetProperty(tc => tc.DeletedAt, DateTime.UtcNow), cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete task {TaskId} and its comments.", taskId);
                throw;
            }

            _logger.LogInformation("Admin {UserId} permanently deleted task {TaskId} from project {ProjectId}.", currentId, taskId, taskResult.Value.ProjectId);
            return Result.Success();
        }



        private async Task<Result<TaskItem>> GetTaskIfMemberAsync(Guid taskId, bool trackChanges, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            IQueryable<TaskItem> query = trackChanges
                ? _context.TaskItems.Include(t => t.Board)
                : _context.TaskItems.AsNoTracking().Include(t => t.Board);

            // Task will taken if IsDeleted is false, filter is created
            TaskItem? task = await query.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
                return Result<TaskItem>.NotFound("Task was not found.");

            bool hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId && p.ProjectMembers.Any(pm => pm.UserId == currentId), cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to access task {TaskId} without membership in project {ProjectId}.", currentId, taskId, task.ProjectId);
                return Result<TaskItem>.NotFound("Task was not found.");
            }

            return Result<TaskItem>.Success(task);
        }


        private async Task<Result> EnsureCanModifyTaskAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            ProjectMember? member = await _context.Projects
                .Where(p => p.Id == task.ProjectId)
                .SelectMany(p => p.ProjectMembers.Where(pm => pm.UserId == currentId))
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                return Result.Forbidden("You are not a member of this project, or it has been deleted.");

            if (member.Role == ProjectRole.Admin)
                return Result.Success();

            if (task.UserId == currentId)
                return Result.Success();

            _logger.LogWarning("User {UserId} attempted to modify task {TaskId} without being the assignee or an Admin.", currentId, task.Id);
            return Result.Forbidden("You can only modify your own tasks, unless you are an administrator.");
        }

        private async Task<Result<Board>> ResolveBoardForProjectAsync(Guid projectId, Guid? boardId, CancellationToken cancellationToken)
        {
            if (boardId.HasValue)
            {
                Board? board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == boardId.Value && b.ProjectId == projectId, cancellationToken);
                return board == null ? Result<Board>.NotFound("Board was not found in this project.") : Result<Board>.Success(board);
            }

            Board? defaultBoard = await _context.Boards.Where(b => b.ProjectId == projectId).OrderBy(b => b.OrderIndex).FirstOrDefaultAsync(cancellationToken);
            return defaultBoard == null ? Result<Board>.NotFound("Project has no boards configured.") : Result<Board>.Success(defaultBoard);
        }
    }
}
