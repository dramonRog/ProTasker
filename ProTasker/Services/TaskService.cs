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

        public TaskService(AppDbContext context, IUserContextService userContextService, IMapper mapper)
        {
            _context = context;
            _userContextService = userContextService;
            _mapper = mapper;
        }

        public async Task<Result<List<TaskResponse>>> GetAllProjectTasksAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken))
                return Result<List<TaskResponse>>.Forbidden("You are not a member of this project.");

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
                return Result<List<TaskResponse>>.Forbidden("You can't get tasks of this user");

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

        public async Task<Result<TaskResponse>> GetByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            if (!await _context.ProjectMembers.AnyAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken))
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            TaskResponse? result = await _context.TaskItems
                .AsNoTracking()
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

            return result == null ? Result<TaskResponse>.NotFound("Task was not found.") : Result<TaskResponse>.Success(result);
        }

        public async Task<Result<TaskResponse>> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == request.ProjectId, cancellationToken); 
            
            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
                return Result<TaskResponse>.Forbidden("Only administrators can create tasks");

            TaskItem task = _mapper.Map<TaskItem>(request);

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> UpdateAsync(Guid projectId, Guid taskId, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);
            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
                return Result<TaskResponse>.Forbidden("Only administrators can alter task data.");

            TaskItem? task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            if (request.Title is not null)
                task.Title = request.Title;

            if (request.Description is not null)
                task.Description = request.Description;

            if (request.DueDate is not null)
                task.DueDate = request.DueDate;

            await _context.SaveChangesAsync(cancellationToken);
            
            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> AssignTaskAsync(Guid projectId, Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            if (member.Role != ProjectRole.Admin && currentId != request.UserId && task.UserId != null)
                return Result<TaskResponse>.Forbidden("Only administrators can assign tasks.");

            if (request.UserId.HasValue)
            {
                ProjectMember? memberAssignee = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == request.UserId && pm.ProjectId == projectId, cancellationToken);

                if (memberAssignee == null)
                    return Result<TaskResponse>.NotFound("Member was not found.");
            }

            task.UserId = request.UserId;
            await _context.SaveChangesAsync(cancellationToken);

            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> ChangeTaskStatusAsync(Guid projectId, Guid taskId, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);
            
            if (member == null)
                return Result<TaskResponse>.Forbidden("You are not a member of this project.");

            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            if (member.Role != ProjectRole.Admin && currentId != task.UserId && task.UserId != null)
                return Result<TaskResponse>.Forbidden("Only administrators can change the task status of other members.");

            task.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);

            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result> DeleteByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
        {
            Guid currentId = _userContextService.GetCurrentUserId();
            ProjectMember? member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == currentId && pm.ProjectId == projectId, cancellationToken);

            if (member == null)
                return Result.Forbidden("You are not a member of this project.");

            if (member.Role != ProjectRole.Admin)
                return Result.Forbidden("Only administrators can remove tasks.");

            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

            if (task != null)
            {
                _context.TaskItems.Remove(task);
                await _context.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }

            return Result.NotFound("Task was not found.");
        }
    }
}
