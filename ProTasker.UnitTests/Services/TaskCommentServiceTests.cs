using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.TaskComment;
using ProTasker.Mapping;
using ProTasker.Models;
using ProTasker.Models.Entities;
using ProTasker.Models.Enums;
using ProTasker.Pagination;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class TaskCommentServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<TaskCommentService>> _loggerMock;
        private readonly Mock<IProjectAccessService> _projectAccessServiceMock;
        private readonly Mock<IUserContextService> _userContextServiceMock;
        private readonly IMapper _mapper;
        private readonly TaskCommentService _taskCommentService;

        public TaskCommentServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(_options);
            _context.Database.EnsureCreated();

            _loggerMock = new Mock<ILogger<TaskCommentService>>();
            _projectAccessServiceMock = new Mock<IProjectAccessService>();
            _userContextServiceMock = new Mock<IUserContextService>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TaskCommentMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _taskCommentService = new TaskCommentService(
                _context,
                _loggerMock.Object,
                _projectAccessServiceMock.Object,
                _userContextServiceMock.Object,
                _mapper);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _connection.Dispose();
        }

        #region GetTaskCommentsAsync Tests

        [Fact]
        public async Task GetTaskCommentsAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            var pagination = new PaginationQuery { PageNumber = 1, PageSize = 10 };
            var result = await _taskCommentService.GetTaskCommentsAsync(Guid.NewGuid(), pagination, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task is not found.");
        }

        [Fact]
        public async Task GetTaskCommentsAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var pagination = new PaginationQuery { PageNumber = 1, PageSize = 10 };
            var result = await _taskCommentService.GetTaskCommentsAsync(taskId, pagination, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task GetTaskCommentsAsync_ShouldReturnPaginatedAndOrderedComments_WhenSuccessful()
        {
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });

            _context.TaskComments.Add(new TaskComment { Id = Guid.NewGuid(), TaskId = taskId, UserId = userId, Title = "Old", Description = "D", CreatedAt = DateTime.UtcNow.AddMinutes(-10) });
            _context.TaskComments.Add(new TaskComment { Id = Guid.NewGuid(), TaskId = taskId, UserId = userId, Title = "Newest", Description = "D", CreatedAt = DateTime.UtcNow });
            _context.TaskComments.Add(new TaskComment { Id = Guid.NewGuid(), TaskId = taskId, UserId = userId, Title = "Middle", Description = "D", CreatedAt = DateTime.UtcNow.AddMinutes(-5) });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            var pagination = new PaginationQuery { PageNumber = 1, PageSize = 2 };
            var result = await _taskCommentService.GetTaskCommentsAsync(taskId, pagination, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(3);
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items[0].Title.Should().Be("Newest");
            result.Value.Items[1].Title.Should().Be("Middle");
        }

        #endregion

        #region GetTaskCommentByIdAsync Tests

        [Fact]
        public async Task GetTaskCommentByIdAsync_ShouldReturnNotFound_WhenCommentDoesNotExist()
        {
            var result = await _taskCommentService.GetTaskCommentByIdAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("This comment was not found.");
        }

        [Fact]
        public async Task GetTaskCommentByIdAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = userId, Title = "C", Description = "D" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var result = await _taskCommentService.GetTaskCommentByIdAsync(commentId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task GetTaskCommentByIdAsync_ShouldReturnComment_WhenSuccessful()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = userId, Title = "Valid Comment", Description = "D" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            var result = await _taskCommentService.GetTaskCommentByIdAsync(commentId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("Valid Comment");
        }

        #endregion

        #region CreateTaskCommentAsync Tests

        [Fact]
        public async Task CreateTaskCommentAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            var request = new CreateTaskCommentRequest("Title", "Description");
            var result = await _taskCommentService.CreateTaskCommentAsync(Guid.NewGuid(), request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task was not found.");
        }

        [Fact]
        public async Task CreateTaskCommentAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var request = new CreateTaskCommentRequest("Title", "Description");
            var result = await _taskCommentService.CreateTaskCommentAsync(taskId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task CreateTaskCommentAsync_ShouldCreateComment_WhenSuccessful()
        {
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _context.Users.Add(new User { Id = currentUserId, Email = "test@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            await _context.SaveChangesAsync();

            var request = new CreateTaskCommentRequest("New Comment", "New Description");
            var result = await _taskCommentService.CreateTaskCommentAsync(taskId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("New Comment");
            result.Value.Description.Should().Be("New Description");

            var dbComment = await _context.TaskComments.FirstAsync(tc => tc.TaskId == taskId);
            dbComment.UserId.Should().Be(currentUserId);
        }

        #endregion

        #region UpdateTaskCommentAsync Tests

        [Fact]
        public async Task UpdateTaskCommentAsync_ShouldReturnNotFound_WhenCommentDoesNotExist()
        {
            var request = new UpdateTaskCommentRequest("Title", "Description");
            var result = await _taskCommentService.UpdateTaskCommentAsync(Guid.NewGuid(), request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Comment was not found.");
        }

        [Fact]
        public async Task UpdateTaskCommentAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = userId, Title = "C", Description = "D" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var request = new UpdateTaskCommentRequest("Title", "Description");
            var result = await _taskCommentService.UpdateTaskCommentAsync(commentId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task UpdateTaskCommentAsync_ShouldReturnForbidden_WhenUserIsNotOwner()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var commentOwnerId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _context.Users.Add(new User { Id = commentOwnerId, Email = "owner@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = commentOwnerId, Title = "C", Description = "D" });
            await _context.SaveChangesAsync();

            var request = new UpdateTaskCommentRequest("Title", "Description");
            var result = await _taskCommentService.UpdateTaskCommentAsync(commentId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can alter only your own comments.");
        }

        [Fact]
        public async Task UpdateTaskCommentAsync_ShouldUpdateFields_WhenValuesAreNotNull()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _context.Users.Add(new User { Id = currentUserId, Email = "owner@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = currentUserId, Title = "Old Title", Description = "Old Desc" });
            await _context.SaveChangesAsync();

            var request = new UpdateTaskCommentRequest("New Title", null);
            var result = await _taskCommentService.UpdateTaskCommentAsync(commentId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("New Title");

            var dbComment = await _context.TaskComments.FindAsync(commentId);
            dbComment!.Title.Should().Be("New Title");
            dbComment.Description.Should().Be("Old Desc");
        }

        #endregion

        #region DeleteCommentAsync Tests

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnNotFound_WhenCommentDoesNotExist()
        {
            var result = await _taskCommentService.DeleteCommentAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("The comment was not found.");
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = userId, Title = "C", Description = "D" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var result = await _taskCommentService.DeleteCommentAsync(commentId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnForbidden_WhenNotOwnerAndNotAdmin()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            _context.Users.Add(new User { Id = otherUserId, Email = "other@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = otherUserId, Title = "C", Description = "D" });
            await _context.SaveChangesAsync();

            var result = await _taskCommentService.DeleteCommentAsync(commentId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can only delete your own comments, unless you are an administrator.");
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldDelete_WhenUserIsAdminButNotOwner()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember { UserId = currentUserId, Role = ProjectRole.Admin }));

            _context.Users.Add(new User { Id = otherUserId, Email = "other@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = otherUserId, Title = "C", Description = "D" });
            await _context.SaveChangesAsync();

            var result = await _taskCommentService.DeleteCommentAsync(commentId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _context.TaskComments.Any(c => c.Id == commentId).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldDelete_WhenUserIsOwner()
        {
            var commentId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _context.Users.Add(new User { Id = currentUserId, Email = "owner@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, BoardId = boardId, Title = "T" });

            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, UserId = currentUserId, Title = "C", Description = "D" });
            await _context.SaveChangesAsync();

            var result = await _taskCommentService.DeleteCommentAsync(commentId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _context.TaskComments.Any(c => c.Id == commentId).Should().BeFalse();
        }

        #endregion
    }
}