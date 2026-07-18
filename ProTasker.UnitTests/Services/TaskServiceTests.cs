using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.Mapping;
using ProTasker.Models;
using ProTasker.Models.Enums;
using ProTasker.Pagination;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class TaskServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<IUserContextService> _userContextServiceMock;
        private readonly Mock<IProjectAccessService> _projectAccessServiceMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<TaskService>> _loggerMock;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(_options);
            _context.Database.EnsureCreated();

            _userContextServiceMock = new Mock<IUserContextService>();
            _projectAccessServiceMock = new Mock<IProjectAccessService>();
            _loggerMock = new Mock<ILogger<TaskService>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TaskMappingProfile>();
                cfg.AddProfile<BoardMappingProfile>(); 
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _taskService = new TaskService(
                _context,
                _userContextServiceMock.Object,
                _projectAccessServiceMock.Object,
                _mapper,
                _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _connection.Dispose();
        }

        #region GetAllProjectTasksAsync Tests

        [Fact]
        public async Task GetAllProjectTasksAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var queryParams = new GetTasksQueryParameters(null, null, null);
            var result = await _taskService.GetAllProjectTasksAsync(projectId, new PaginationQuery(), queryParams, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task GetAllProjectTasksAsync_ShouldApplyAllFiltersAndReturnTasks_WhenSuccessful()
        {
            var projectId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid(); 

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _context.Users.Add(new User { Id = assigneeId, Email = "test@test.com", PasswordHash = "hash" });
            _context.Users.Add(new User { Id = otherUserId, Email = "other@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "B" });
            
            _context.TaskItems.Add(new TaskItem { Id = Guid.NewGuid(), ProjectId = projectId, UserId = assigneeId, Priority = TaskPriority.High, Title = "Match this", Description = "Desc" });
            _context.TaskItems.Add(new TaskItem { Id = Guid.NewGuid(), ProjectId = projectId, UserId = assigneeId, Priority = TaskPriority.Unassigned, Title = "Match this too" });
            _context.TaskItems.Add(new TaskItem { Id = Guid.NewGuid(), ProjectId = projectId, UserId = otherUserId, Priority = TaskPriority.High, Title = "Match this" });
            await _context.SaveChangesAsync();

            var queryParams = new GetTasksQueryParameters(TaskPriority.High, assigneeId, null);

            var result = await _taskService.GetAllProjectTasksAsync(projectId, new PaginationQuery { PageNumber = 1, PageSize = 10 }, queryParams, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(1); 
        }

        #endregion

        #region GetAllUserTasksAsync Tests

        [Fact]
        public async Task GetAllUserTasksAsync_ShouldReturnForbidden_WhenRequestingOtherUserTasksWithoutProject()
        {
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());
            
            var queryParams = new GetTasksQueryParameters(null, null, null);
            var result = await _taskService.GetAllUserTasksAsync(null, Guid.NewGuid(), new PaginationQuery(), queryParams, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can't get tasks of this user");
        }

        [Fact]
        public async Task GetAllUserTasksAsync_ShouldReturnForbidden_WhenProjectProvidedButUserIsNotMemberOfIt()
        {
            var projectId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Forbidden("Not member"));

            var queryParams = new GetTasksQueryParameters(null, null, null);
            var result = await _taskService.GetAllUserTasksAsync(projectId, Guid.NewGuid(), new PaginationQuery(), queryParams, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not member");
        }

        [Fact]
        public async Task GetAllUserTasksAsync_ShouldReturnNotFound_WhenTargetUserIsNotInProject()
        {
            var projectId = Guid.NewGuid();
            var currentId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Users.Add(new User { Id = targetId, Email = "test@test.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var queryParams = new GetTasksQueryParameters(null, null, null);
            var result = await _taskService.GetAllUserTasksAsync(projectId, targetId, new PaginationQuery(), queryParams, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User was not found as a member of this project.");
        }

        [Fact]
        public async Task GetAllUserTasksAsync_ShouldReturnTasks_FromVisibleProjectsOnly()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var hiddenProjectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "Visible" });
            _context.Projects.Add(new Project { Id = hiddenProjectId, Name = "Hidden" });
            
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });

            _context.TaskItems.Add(new TaskItem { Id = Guid.NewGuid(), ProjectId = projectId, UserId = currentId, Title = "T1", Priority = TaskPriority.High });
            _context.TaskItems.Add(new TaskItem { Id = Guid.NewGuid(), ProjectId = hiddenProjectId, UserId = currentId, Title = "T2", Priority = TaskPriority.High });
            await _context.SaveChangesAsync();

            var queryParams = new GetTasksQueryParameters(TaskPriority.High, null, null);
            var result = await _taskService.GetAllUserTasksAsync(null, currentId, new PaginationQuery { PageNumber = 1, PageSize = 10 }, queryParams, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(1); 
            result.Value.Items.First().Title.Should().Be("T1");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());
            var result = await _taskService.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenUserIsNotMemberOfTaskProject()
        {
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var currentId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, Title = "T" });
            await _context.SaveChangesAsync();

            var result = await _taskService.GetByIdAsync(taskId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task was not found."); 
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTask_WhenUserHasAccess()
        {
            var taskId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var currentId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, Title = "T" });
            await _context.SaveChangesAsync();

            var result = await _taskService.GetByIdAsync(taskId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("T");
        }

        #endregion

        #region CreateAsync Tests
        [Fact]
        public async Task CreateAsync_ShouldCreateTask_WithExplicitBoard()
        {
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "Specific Board", OrderIndex = 1 });
            await _context.SaveChangesAsync();

            var request = new CreateTaskItemRequest("New Task", null, null, projectId, TaskPriority.Unassigned, null, boardId);
            var result = await _taskService.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("New Task");

            var dbTask = await _context.TaskItems.FirstAsync();
            dbTask.BoardId.Should().Be(boardId);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var request = new CreateTaskItemRequest("Title", null, null, Guid.NewGuid(), TaskPriority.Unassigned, null, null);
            var result = await _taskService.CreateAsync(request, CancellationToken.None);
            
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnNotFound_WhenAssigneeIsNotMember()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            var request = new CreateTaskItemRequest("Title", null, null, projectId, TaskPriority.Unassigned, Guid.NewGuid(), null);
            var result = await _taskService.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Assigned user is not a member of this project.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnNotFound_WhenExplicitBoardIsMissing()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            var request = new CreateTaskItemRequest("Title", null, null, projectId, TaskPriority.Unassigned, null, Guid.NewGuid());
            var result = await _taskService.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Board was not found in this project.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnNotFound_WhenNoDefaultBoardExists()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            var request = new CreateTaskItemRequest("Title", null, null, projectId, TaskPriority.Unassigned, null, null);
            var result = await _taskService.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project has no boards configured.");
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateTask_WithDefaultBoard()
        {
            var projectId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B", OrderIndex = 0 });
            await _context.SaveChangesAsync();

            var request = new CreateTaskItemRequest("New Task", null, null, projectId, TaskPriority.Unassigned, null, null);
            var result = await _taskService.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("New Task");
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldReturnForbidden_WhenUserCannotModifyTask()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid(); 

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = otherUserId, Email = "other@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member }); 
            
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = otherUserId, Title = "T" }); 
            await _context.SaveChangesAsync();

            var request = new UpdateTaskItemRequest("New", null, null, null);
            var result = await _taskService.UpdateAsync(taskId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can only modify your own tasks, unless you are an administrator.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTask_WhenUserIsAssignee()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });
            
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = currentId, Title = "Old", Priority = TaskPriority.Unassigned }); 
            await _context.SaveChangesAsync();

            var request = new UpdateTaskItemRequest("New", "Desc", TaskPriority.High, DateTime.UtcNow);
            var result = await _taskService.UpdateAsync(taskId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("New");
            result.Value.Priority.Should().Be(TaskPriority.High);
        }

        #endregion

        #region AssignTaskAsync Tests

        [Fact]
        public async Task AssignTaskAsync_ShouldReturnForbidden_WhenNotAdminAssignsToSomeoneElse()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = targetId, Email = "t@t.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = currentId, Title = "T" }); 
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _taskService.AssignTaskAsync(taskId, new AssignTaskRequest(targetId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can only assign tasks to yourself.");
        }

        [Fact]
        public async Task AssignTaskAsync_ShouldReturnForbidden_WhenNotAdminUnassignsSomeoneElse()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = otherUserId, Email = "b@b.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = otherUserId, Title = "T" }); 
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _taskService.AssignTaskAsync(taskId, new AssignTaskRequest(null), CancellationToken.None); 

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can only unassign your own tasks.");
        }

        [Fact]
        public async Task AssignTaskAsync_ShouldReturnForbidden_WhenStealingTask()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = otherUserId, Email = "b@b.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = otherUserId, Title = "T" }); 
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _taskService.AssignTaskAsync(taskId, new AssignTaskRequest(currentId), CancellationToken.None); 

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("This task is already assigned to another user.");
        }

        [Fact]
        public async Task AssignTaskAsync_ShouldAssign_WhenAdminAssignsSomeoneElse()
        {
            var currentId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = targetId, Email = "t@t.com", PasswordHash = "h" }); 
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Admin });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = null, Title = "T" }); 
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var result = await _taskService.AssignTaskAsync(taskId, new AssignTaskRequest(targetId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            var dbTask = await _context.TaskItems.FindAsync(taskId);
            dbTask!.UserId.Should().Be(targetId);
        }

        #endregion

        #region MoveTaskToBoardAsync Tests

        [Fact]
        public async Task MoveTaskToBoardAsync_ShouldReturnNotFound_WhenBoardIsInvalid()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Admin });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, Title = "T" }); 
            await _context.SaveChangesAsync();

            var request = new MoveTaskToBoardRequest(Guid.NewGuid()); 
            var result = await _taskService.MoveTaskToBoardAsync(taskId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Board was not found in this project.");
        }

        [Fact]
        public async Task MoveTaskToBoardAsync_ShouldMoveTask_WhenSuccessful()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "Done", OrderIndex = 0 });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Admin });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, Title = "T" }); 
            await _context.SaveChangesAsync();

            var request = new MoveTaskToBoardRequest(boardId);
            var result = await _taskService.MoveTaskToBoardAsync(taskId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dbTask = await _context.TaskItems.FindAsync(taskId);
            dbTask!.BoardId.Should().Be(boardId);
        }

        #endregion

        #region ChangeTaskPriorityAsync Tests

        [Fact]
        public async Task ChangeTaskPriorityAsync_ShouldReturnNotFound_WhenTaskDoesNotExistOrNoAccess()
        {
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            var request = new ChangeTaskPriorityRequest(TaskPriority.High);
            var result = await _taskService.ChangeTaskPriorityAsync(Guid.NewGuid(), request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task was not found.");
        }

        [Fact]
        public async Task ChangeTaskPriorityAsync_ShouldReturnForbidden_WhenUserCannotModifyTask()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = otherUserId, Email = "other@test.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });

            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = otherUserId, Title = "T" });
            await _context.SaveChangesAsync();

            var request = new ChangeTaskPriorityRequest(TaskPriority.High);
            var result = await _taskService.ChangeTaskPriorityAsync(taskId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You can only modify your own tasks, unless you are an administrator.");
        }

        [Fact]
        public async Task ChangeTaskPriorityAsync_ShouldChangePriority_WhenUserIsAssignee()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });

            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = currentId, Title = "Old", Priority = TaskPriority.Low });
            await _context.SaveChangesAsync();

            var request = new ChangeTaskPriorityRequest(TaskPriority.High);
            var result = await _taskService.ChangeTaskPriorityAsync(taskId, request, CancellationToken.None);
            
            result.IsSuccess.Should().BeTrue();
            result.Value!.Priority.Should().Be(TaskPriority.High);

            var dbTask = await _context.TaskItems.FindAsync(taskId);
            dbTask!.Priority.Should().Be(TaskPriority.High);
        }

        #endregion

        #region DeleteByIdAsync Tests

        [Fact]
        public async Task DeleteByIdAsync_ShouldReturnForbidden_WhenNotAdmin()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Member });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, Title = "T" }); 
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _taskService.DeleteByIdAsync(taskId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldHardDeleteTask_AndSoftDeleteComments()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);

            _context.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Admin });
            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, Title = "T" }); 
            _context.TaskComments.Add(new TaskComment { Id = commentId, TaskId = taskId, Title = "Com", Description = "D", IsDeleted = false });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var result = await _taskService.DeleteByIdAsync(taskId, CancellationToken.None);

            _context.ChangeTracker.Clear();

            result.IsSuccess.Should().BeTrue();
            
            var dbTask = await _context.TaskItems.FindAsync(taskId);
            dbTask.Should().BeNull(); 

            var dbComment = await _context.TaskComments.IgnoreQueryFilters().FirstAsync(c => c.Id == commentId);
            dbComment.IsDeleted.Should().BeTrue(); 
            dbComment.DeletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldRollbackAndThrow_OnException()
        {
            var currentId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentId);
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            using var faultyContext = new FaultyDbContext(_options);
            faultyContext.Database.EnsureCreated();
            
            faultyContext.Users.Add(new User { Id = currentId, Email = "a@a.com", PasswordHash = "h" });
            faultyContext.Projects.Add(new Project { Id = projectId, Name = "P" });
            faultyContext.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = currentId, Role = ProjectRole.Admin });
            faultyContext.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, Title = "T" });
            await faultyContext.SaveChangesAsync();

            var faultyService = new TaskService(
                faultyContext, _userContextServiceMock.Object, _projectAccessServiceMock.Object, _mapper, _loggerMock.Object);

            Func<Task> act = async () => await faultyService.DeleteByIdAsync(taskId, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>().WithMessage("Simulated DB failure");
        }

        #endregion

        private class FaultyDbContext : AppDbContext
        {
            private bool _failOnSave = false;

            public FaultyDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (_failOnSave)
                    throw new Exception("Simulated DB failure");

                _failOnSave = true;
                return base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}