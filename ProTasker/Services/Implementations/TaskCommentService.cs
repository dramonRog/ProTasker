using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.TaskComment;
using ProTasker.DTOs.Responses.TaskComment;
using ProTasker.Models;
using ProTasker.Services.Interfaces;

namespace ProTasker.Services.Implementations
{
    public class TaskCommentService : ITaskCommentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TaskCommentService> _logger;
        private readonly IProjectAccessService _projectAccessService;
        private readonly IUserContextService _userContextService;
        private readonly IMapper _mapper;

        public TaskCommentService(AppDbContext context, ILogger<TaskCommentService> logger, IProjectAccessService projectAccessService, IUserContextService userContextService, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _projectAccessService = projectAccessService;
            _userContextService = userContextService;
            _mapper = mapper;
        }

        public async Task<Result<List<TaskCommentResponse>>> GetTaskCommentsAsync(Guid taskId, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
                return Result<List<TaskCommentResponse>>.NotFound("Task is not found.");

            Result hasAccess = await _projectAccessService.EnsureMemberAsync(task.ProjectId, cancellationToken);
            if (!hasAccess.IsSuccess)
                return Result<List<TaskCommentResponse>>.Forbidden(hasAccess.Error);

            List<TaskComment> comments = await _context.TaskComments
                .AsNoTracking()
                .Include(tc => tc.User)
                .Where(tc => tc.TaskId == taskId)
                .OrderBy(tc => tc.CreatedAt)
                .ToListAsync(cancellationToken);
            return Result<List<TaskCommentResponse>>.Success(_mapper.Map<List<TaskCommentResponse>>(comments));
        }

        public async Task<Result<TaskCommentResponse>> GetTaskCommentByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            TaskComment? comment = await _context.TaskComments
                .AsNoTracking()
                .Include(tc => tc.Task)
                .Include(tc => tc.User)
                .FirstOrDefaultAsync(tc => tc.Id == id, cancellationToken);

            if (comment == null)
                return Result<TaskCommentResponse>.NotFound("This comment was not found.");

            Result hasAccess = await _projectAccessService.EnsureMemberAsync(comment.Task!.ProjectId, cancellationToken);
            if (!hasAccess.IsSuccess)
                return Result<TaskCommentResponse>.Forbidden(hasAccess.Error);

            return Result<TaskCommentResponse>.Success(_mapper.Map<TaskCommentResponse>(comment));
        }

        public async Task<Result<TaskCommentResponse>> CreateTaskCommentAsync(Guid taskId, CreateTaskCommentRequest request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
            if (task == null)
                return Result<TaskCommentResponse>.NotFound("Task was not found.");

            Result hasAccess = await _projectAccessService.EnsureMemberAsync(task.ProjectId, cancellationToken);
            if (!hasAccess.IsSuccess)
                return Result<TaskCommentResponse>.Forbidden(hasAccess.Error);

            TaskComment comment = new TaskComment
            {
                Title = request.Title,
                Description = request.Description,
                Task = task,
                TaskId = taskId,
                UserId = currentUserId
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(comment).Reference(c => c.User).LoadAsync(cancellationToken);

            return Result<TaskCommentResponse>.Success(_mapper.Map<TaskCommentResponse>(comment));
        }

        public async Task<Result<TaskCommentResponse>> UpdateTaskCommentAsync(Guid id, UpdateTaskCommentRequest request, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();
            TaskComment? comment = await _context.TaskComments
                .Include(tc => tc.Task)
                .Include(tc => tc.User)
                .FirstOrDefaultAsync(tc => tc.Id == id, cancellationToken);

            if (comment == null)
                return Result<TaskCommentResponse>.NotFound("Comment was not found.");

            Result hasAccess = await _projectAccessService.EnsureMemberAsync(comment.Task!.ProjectId, cancellationToken);
            if (!hasAccess.IsSuccess)
                return Result<TaskCommentResponse>.Forbidden(hasAccess.Error);

            if (comment.UserId != currentUserId)
                return Result<TaskCommentResponse>.Forbidden("You can alter only your own comments.");

            if (request.Title != null)
                comment.Title = request.Title;
            if (request.Description != null)
                comment.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);

            return Result<TaskCommentResponse>.Success(_mapper.Map<TaskCommentResponse>(comment));
        }

        public async Task<Result> DeleteCommentAsync(Guid id, CancellationToken cancellationToken)
        {
            Guid currentUserId = _userContextService.GetCurrentUserId();
            TaskComment? comment = await _context.TaskComments.Include(tc => tc.Task).FirstOrDefaultAsync(tc => tc.Id == id, cancellationToken);

            if (comment == null)
                return Result.NotFound("The comment was not found.");

            Result hasAccess = await _projectAccessService.EnsureMemberAsync(comment.Task!.ProjectId, cancellationToken);
            if (!hasAccess.IsSuccess)
                return hasAccess;

            if (comment.UserId != currentUserId)
            {
                Result adminAccess = await _projectAccessService.EnsureAdminAsync(comment.Task!.ProjectId, cancellationToken);
                if (!adminAccess.IsSuccess)
                    return Result.Forbidden("You can only delete your own comments, unless you are an administrator.");
            }

            _context.TaskComments.Remove(comment);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
