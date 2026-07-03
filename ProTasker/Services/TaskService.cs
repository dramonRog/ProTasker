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
        private readonly IMapper _mapper;

        public TaskService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<List<TaskResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            var taskResult = await _context.TaskItems
                .AsNoTracking()
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<TaskResponse>>.Success(taskResult);
        }

        public async Task<Result<TaskResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            TaskResponse? result = await _context.TaskItems
                .AsNoTracking()
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            return result == null ? Result<TaskResponse>.NotFound("Task was not found.") : Result<TaskResponse>.Success(result);
        }

        public async Task<Result<TaskResponse>> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            TaskItem task = _mapper.Map<TaskItem>(request);

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> UpdateAsync(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

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

        public async Task<Result<TaskResponse>> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            if (!await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
                return Result<TaskResponse>.NotFound("User was not found.");

            task.UserId = request.UserId;
            await _context.SaveChangesAsync(cancellationToken);

            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result<TaskResponse>> ChangeTaskStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task == null)
                return Result<TaskResponse>.NotFound("Task was not found.");

            task.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);

            TaskResponse response = _mapper.Map<TaskResponse>(task);
            return Result<TaskResponse>.Success(response);
        }

        public async Task<Result> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task != null)
            {
                _context.TaskItems.Remove(task);
                await _context.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }

            return Result.NotFound();
        }
    }
}
