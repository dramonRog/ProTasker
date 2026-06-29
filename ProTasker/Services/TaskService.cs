using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
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

        public async Task<List<TaskResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.TaskItems
                .AsNoTracking()
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            TaskResponse? result = await _context.TaskItems
                .AsNoTracking()
                .ProjectTo<TaskResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            return result;
        }

        public async Task<TaskResponse> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken)
        {
            TaskItem task = _mapper.Map<TaskItem>(request);

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TaskResponse>(task);
        }

        public async Task<TaskResponse?> UpdateAsync(Guid Id, UpdateTaskItemRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == Id, cancellationToken);

            if (task == null)
                return null;

            if (request.Title is not null)
                task.Title = request.Title;

            if (request.Description is not null)
                task.Description = request.Description;

            if (request.DueDate is not null)
                task.DueDate = request.DueDate;

            await _context.SaveChangesAsync(cancellationToken);
            
            return _mapper.Map<TaskResponse>(task);
        }

        public async Task<TaskResponse?> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task != null && await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
            {
                task.UserId = request.UserId;
                await _context.SaveChangesAsync(cancellationToken);
                
                return _mapper.Map<TaskResponse>(task);
            }

            return null;
        }

        public async Task<TaskResponse?> ChangeTaskStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task == null)
                return null;

            task.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TaskResponse>(task);
        }

        public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            TaskItem? task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task != null)
            {
                _context.TaskItems.Remove(task);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }

            return false;
        }
    }
}
