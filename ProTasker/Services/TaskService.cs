using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;
using ProTasker.Models;

namespace ProTasker.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;

        public TaskService(AppDbContext context, IUserContextService userContextService, IMapper mapper, ILogger<TaskService> logger)
        {
            _context = context;
            _userContextService = userContextService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<TaskResponse>>> GetAllProjectTasksAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken))
            {
                _logger.LogWarning("User {UserId} attempted to view tasks for project {ProjectId} without being a member.", currentId, projectId);
                return Result<List<TaskResponse>>.Forbidden("You are not a member of this project.");
            }

            var taskResult = await _context.TaskItems
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId)
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<TaskResponse>>.Success(taskResult);
        }

        public async Task<Result<List<TaskResponse>>> GetAllUserTasksAsync(Guid? projectId, Guid userId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();

            if (currentId != userId && projectId == null)
            {
                _logger.LogWarning("User {UserId} attempted to view all tasks of user {TargetUserId} without specifying a shared project context.", currentId, userId);
                return Result<List<TaskResponse>>.Forbidden("You can't get tasks of this user");
            }

            if (projectId != null)
            {
                if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken))
                    return Result<List<TaskResponse>>.Forbidden("You are not a member of this project.");

                if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, cancellationToken))
                    return Result<List<TaskResponse>>.NotFound("User was not found as a member of this project.");
            }

            var query = _context.TaskItems.AsNoTracking().Where(t => t.UserId == userId);

            if (projectId != null)
                query = query.Where(t => t.ProjectId == projectId.Value);

            List<TaskResponse> tasks = await query.ProjectTo<TaskResponse>(_mapper.ConfigurationProvider).ToListAsync();
            return Result<List<TaskResponse>>.Success(tasks);
        }

        public async Task<Result<TaskResponse>> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
        {
            TaskItem? result = await _context.TaskItems
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (result == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            Guid currentId = _userContextService.GetCurrentUserId();
            if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == currentId && pm.ProjectId == result.ProjectId, cancellationToken))
            {
                _logger.LogWarning("User {UserId} attempted to access task {TaskId} in project {ProjectId} without being a member.", currentId, taskId, result.ProjectId);
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");
            }

            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(result));
        }

        public async Task<Result<TaskResponse>> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == request.ProjectId, cancellationToken); 
            
            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to create a task in project {ProjectId} without Admin role.", currentId, request.ProjectId);
                return Result<TaskResponse>.Forbidden("Only administrators can create tasks");
            }

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

            TaskItem task = _mapper.Map<TaskItem>(request);
            task.UserId = request.AssignedUserId;

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {UserId} successfully created task {TaskId} in project {ProjectId}.", currentId, task.Id, request.ProjectId);

            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> UpdateAsync(Guid taskId, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == task.ProjectId, cancellationToken);

            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to update task {TaskId} without Admin role.", currentId, taskId);
                return Result<TaskResponse>.Forbidden("Only administrators can alter task data.");
            }

            if (request.Title is not null)
                task.Title = request.Title;

            if (request.Description is not null)
                task.Description = request.Description;

            if (request.DueDate is not null)
                task.DueDate = request.DueDate;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {UserId} successfully updated data for task {TaskId}.", currentId, taskId);
            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(task));
        }

        public async Task<Result<TaskResponse>> AssignTaskAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == task.ProjectId, cancellationToken);

            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin && currentId != request.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to assign task {TaskId} to user {TargetUserId} without Admin role.", currentId, taskId, request.UserId);
                return Result<TaskResponse>.Forbidden("Only administrators can assign tasks to other members.");
            }

            if (request.UserId.HasValue)
            {
                bool isAssigneeMember = await _context.ProjectMembers.AnyAsync(pm => pm.UserId == request.UserId && pm.ProjectId == task.ProjectId, cancellationToken);

                if (!isAssigneeMember)
                    return Result<TaskResponse>.NotFound("Assigned member was not found in the project.");
            }

            task.UserId = request.UserId;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully assigned task {TaskId} to user {TargetUserId}.", currentId, taskId, request.UserId);
            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(task));
        }

        public async Task<Result<TaskResponse>> ChangeTaskStatusAsync(Guid taskId, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == task.ProjectId, cancellationToken);

            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin && currentId != task.UserId && task.UserId != null)
            {
                _logger.LogWarning("User {UserId} attempted to change status of task {TaskId} (assigned to {AssignedUserId}) without Admin role.", currentId, taskId, task.UserId);
                return Result<TaskResponse>.Forbidden("Only administrators can change the task status of other members.");
            }

            task.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully changed status of task {TaskId} to {NewStatus}.", currentId, taskId, request.Status);
            return Result<TaskResponse>.Success(_mapper.Map<TaskResponse>(task));
        }

        public async Task<Result> DeleteByIdAsync(Guid taskId, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
            if (task == null)
                return Result.NotFound("Task was not found.");

            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == task.ProjectId, cancellationToken);

            if (member == null)
                return Result.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
            {
                _logger.LogWarning("User {UserId} attempted to delete task {TaskId} without Admin role.", currentId, taskId);
                return Result.Forbidden("Only administrators can remove tasks.");
            }

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin {UserId} permanently deleted task {TaskId} from project {ProjectId}.", currentId, taskId, task.ProjectId);
            return Result.Success();
        }
    }
}
