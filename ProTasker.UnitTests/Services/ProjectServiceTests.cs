using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.Project;
using ProTasker.Mapping; 
using ProTasker.Models;
using ProTasker.Models.Enums;
using ProTasker.Pagination;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<IUserContextService> _userContextServiceMock;
        private readonly Mock<IProjectAccessService> _projectAccessServiceMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<ProjectService>> _loggerMock;
        private readonly ProjectService _projectService;

        public ProjectServiceTests()
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
            _loggerMock = new Mock<ILogger<ProjectService>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<BoardMappingProfile>();
                cfg.AddProfile<ProjectMappingProfile>();
                cfg.AddProfile<TaskCommentMappingProfile>();
                cfg.AddProfile<TaskMappingProfile>();
                cfg.AddProfile<UserMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            _mapper = mapperConfig.CreateMapper();

            _projectService = new ProjectService(
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

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnProjects_WhereUserIsMember()
        {
            var userId = Guid.NewGuid();
            var myProjectId = Guid.NewGuid();
            var foreignProjectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });

            _context.Projects.Add(new Project { Id = myProjectId, Name = "My Project" });
            _context.Projects.Add(new Project { Id = foreignProjectId, Name = "Foreign Project" });

            _context.ProjectMembers.Add(new ProjectMember { UserId = userId, ProjectId = myProjectId, Role = ProjectRole.Member });
            await _context.SaveChangesAsync();

            var pagination = new PaginationQuery { PageNumber = 1, PageSize = 10 };

            var result = await _projectService.GetAllAsync(pagination, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(1);
            result.Value.Items.Should().ContainSingle(p => p.Id == myProjectId);
        }

        [Fact]
        public async Task GetAllAsync_ShouldApplyPaginationCorrectly()
        {
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });

            for (int i = 0; i < 5; i++)
            {
                var projectId = Guid.NewGuid();
                _context.Projects.Add(new Project { Id = projectId, Name = $"Project {i}", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
                _context.ProjectMembers.Add(new ProjectMember { UserId = userId, ProjectId = projectId, Role = ProjectRole.Member });
            }
            await _context.SaveChangesAsync();

            var pagination = new PaginationQuery { PageNumber = 2, PageSize = 2 };

            var result = await _projectService.GetAllAsync(pagination, CancellationToken.None);

            result.Value!.TotalCount.Should().Be(5);
            result.Value.Items.Should().HaveCount(2);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var result = await _projectService.GetByIdAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenProjectDoesNotExistInDb()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            var result = await _projectService.GetByIdAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project was not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnProjectDetails_WhenSuccessful()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            _context.Projects.Add(new Project { Id = projectId, Name = "Test Project" });
            await _context.SaveChangesAsync();

            var result = await _projectService.GetByIdAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Id.Should().Be(projectId);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldCreateProject_WithAdminRoleAndDefaultBoards()
        {
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var request = new CreateProjectRequest("New Project", "Description");

            var result = await _projectService.CreateAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            var projectId = result.Value!.Id;
            var savedProject = await _context.Projects
                .Include(p => p.ProjectMembers)
                .Include(p => p.Boards)
                .FirstAsync(p => p.Id == projectId);

            savedProject.Name.Should().Be("New Project");
            savedProject.Description.Should().Be("Description");

            savedProject.ProjectMembers.Should().ContainSingle();
            savedProject.ProjectMembers.First().UserId.Should().Be(userId);
            savedProject.ProjectMembers.First().Role.Should().Be(ProjectRole.Admin);

            savedProject.Boards.Should().HaveCount(3);
            savedProject.Boards.Select(b => b.Name).Should().Contain(new[] { "To Do", "In Progress", "Done" });
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var request = new UpdateProjectRequest("Name", "Desc");
            var result = await _projectService.UpdateAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var request = new UpdateProjectRequest("Name", "Desc");
            var result = await _projectService.UpdateAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project was not found.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFields_WhenValuesAreNotNull()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Projects.Add(new Project { Id = projectId, Name = "Old Name", Description = "Old Desc" });
            await _context.SaveChangesAsync();

            var request = new UpdateProjectRequest("New Name", null);

            var result = await _projectService.UpdateAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            var updatedProject = await _context.Projects.FindAsync(projectId);
            updatedProject!.Name.Should().Be("New Name");
            updatedProject.Description.Should().Be("Old Desc");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _projectService.DeleteAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var result = await _projectService.DeleteAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project was not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldHardDeleteProject_AndSoftDeleteTasksAndComments()
        {
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var commentId = Guid.NewGuid();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var project = new Project { Id = projectId, Name = "To Delete" };
            var task = new TaskItem { Id = taskId, ProjectId = projectId, Title = "Task", IsDeleted = false };
            var comment = new TaskComment { Id = commentId, TaskId = taskId, Title = "Comment", Description = "D", IsDeleted = false };

            _context.Projects.Add(project);
            _context.TaskItems.Add(task);
            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await _projectService.DeleteAsync(projectId, CancellationToken.None);

            _context.ChangeTracker.Clear();

            result.IsSuccess.Should().BeTrue();

            var deletedProject = await _context.Projects.FindAsync(projectId);
            deletedProject.Should().BeNull(); 

            var softDeletedTask = await _context.TaskItems.IgnoreQueryFilters().FirstAsync(t => t.Id == taskId);
            softDeletedTask.IsDeleted.Should().BeTrue();
            softDeletedTask.DeletedAt.Should().NotBeNull();

            var softDeletedComment = await _context.TaskComments.IgnoreQueryFilters().FirstAsync(c => c.Id == commentId);
            softDeletedComment.IsDeleted.Should().BeTrue();
            softDeletedComment.DeletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRollbackTransaction_AndThrow_WhenExceptionOccurs()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            using var faultyContext = new FaultyDbContext(_options);
            faultyContext.Database.EnsureCreated();
            faultyContext.Projects.Add(new Project { Id = projectId, Name = "Project" });
            await faultyContext.SaveChangesAsync();

            var service = new ProjectService(faultyContext, _userContextServiceMock.Object, _projectAccessServiceMock.Object, _mapper, _loggerMock.Object);

            Func<Task> act = async () => await service.DeleteAsync(projectId, CancellationToken.None);

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