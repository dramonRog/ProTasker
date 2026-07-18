using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.Mapping;
using ProTasker.Models;
using ProTasker.Models.Enums;
using ProTasker.Pagination;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class ProjectMemberServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly Mock<IUserContextService> _userContextServiceMock;
        private readonly Mock<IProjectAccessService> _projectAccessServiceMock;
        private readonly Mock<ILogger<ProjectMemberService>> _loggerMock;
        private readonly ProjectMemberService _service;

        public ProjectMemberServiceTests()
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
            _loggerMock = new Mock<ILogger<ProjectMemberService>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProjectMappingProfile>();
                cfg.AddProfile<UserMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _service = new ProjectMemberService(
                _context,
                _mapper,
                _userContextServiceMock.Object,
                _projectAccessServiceMock.Object,
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
        public async Task GetAllAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var pagination = new PaginationQuery { PageNumber = 1, PageSize = 10 };
            var result = await _service.GetAllAsync(projectId, pagination, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPaginatedMembers_WhenSuccessful()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });

            for (int i = 0; i < 5; i++)
            {
                var user = new User { Id = Guid.NewGuid(), FirstName = $"U{i}", LastName = "L", Email = $"{i}@test.com", PasswordHash = "hash" };
                _context.Users.Add(user);
                _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = user.Id, Role = ProjectRole.Member, AddedAt = DateTime.UtcNow.AddMinutes(i) });
            }
            await _context.SaveChangesAsync();

            var pagination = new PaginationQuery { PageNumber = 2, PageSize = 2 };
            var result = await _service.GetAllAsync(projectId, pagination, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(5);
            result.Value.Items.Should().HaveCount(2);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Forbidden"));

            var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Forbidden");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenMemberDoesNotExist()
        {
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project member was not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMember_WhenSuccessful()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            var user = new User { Id = userId, FirstName = "A", LastName = "B", Email = "a@a.com", PasswordHash = "hash" };
            _context.Users.Add(user);
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId, Role = ProjectRole.Admin });
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(userId, projectId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Role.Should().Be(ProjectRole.Admin);
        }

        #endregion

        #region AddProjectMemberToProjectAsync Tests

        [Fact]
        public async Task AddProjectMember_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _service.AddProjectMemberToProjectAsync(Guid.NewGuid(), new AddProjectMemberRequest(Guid.NewGuid(), ProjectRole.Member), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task AddProjectMember_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var result = await _service.AddProjectMemberToProjectAsync(projectId, new AddProjectMemberRequest(Guid.NewGuid(), ProjectRole.Member), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User was not found.");
        }

        [Fact]
        public async Task AddProjectMember_ShouldReturnConflict_WhenUserIsAlreadyMember()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var user = new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" };
            _context.Users.Add(user);
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId });
            await _context.SaveChangesAsync();

            var result = await _service.AddProjectMemberToProjectAsync(projectId, new AddProjectMemberRequest(userId, ProjectRole.Member), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is already a member of this project.");
        }

        [Fact]
        public async Task AddProjectMember_ShouldReturnSuccess_WhenDataIsValid()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var user = new User { Id = userId, FirstName = "T", LastName = "T", Email = "test@test.com", PasswordHash = "hash" };
            _context.Users.Add(user);
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            await _context.SaveChangesAsync();

            var result = await _service.AddProjectMemberToProjectAsync(projectId, new AddProjectMemberRequest(userId, ProjectRole.Admin), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Role.Should().Be(ProjectRole.Admin);

            var dbMember = await _context.ProjectMembers.FirstAsync(pm => pm.UserId == userId && pm.ProjectId == projectId);
            dbMember.Role.Should().Be(ProjectRole.Admin);
        }

        #endregion

        #region ChangeProjectMemberRoleAsync Tests

        [Fact]
        public async Task ChangeRole_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _service.ChangeProjectMemberRoleAsync(Guid.NewGuid(), Guid.NewGuid(), new ChangeProjectMemberRole(ProjectRole.Member), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task ChangeRole_ShouldReturnNotFound_WhenTargetMemberDoesNotExist()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var result = await _service.ChangeProjectMemberRoleAsync(Guid.NewGuid(), projectId, new ChangeProjectMemberRole(ProjectRole.Member), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Member was not found.");
        }

        [Fact]
        public async Task ChangeRole_ShouldReturnSuccessDirectly_WhenRoleIsAlreadySame()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Users.Add(new User { Id = userId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId, Role = ProjectRole.Member });
            await _context.SaveChangesAsync();

            var result = await _service.ChangeProjectMemberRoleAsync(userId, projectId, new ChangeProjectMemberRole(ProjectRole.Member), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ChangeRole_ShouldReturnConflict_WhenDemotingLastAdmin()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Users.Add(new User { Id = userId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });

            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId, Role = ProjectRole.Admin });
            await _context.SaveChangesAsync();

            var result = await _service.ChangeProjectMemberRoleAsync(userId, projectId, new ChangeProjectMemberRole(ProjectRole.Member), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("In the project must be at least one administrator.");
        }

        [Fact]
        public async Task ChangeRole_ShouldChangeRole_WhenValid()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Users.Add(new User { Id = userId, Email = "a@a.com", PasswordHash = "h" });
            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId, Role = ProjectRole.Member });
            await _context.SaveChangesAsync();

            var result = await _service.ChangeProjectMemberRoleAsync(userId, projectId, new ChangeProjectMemberRole(ProjectRole.Admin), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Role.Should().Be(ProjectRole.Admin);
        }

        #endregion

        #region DeleteByIdAsync Tests

        [Fact]
        public async Task DeleteByIdAsync_ShouldReturnForbidden_WhenRequesterIsNotInProject()
        {
            var projectId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteByIdAsync(Guid.NewGuid(), projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You are not a member of this project, or it has been deleted.");
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldReturnForbidden_WhenRequesterIsMemberButDeletesSomeoneElse()
        {
            var requesterId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(requesterId);

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Users.Add(new User { Id = requesterId, Email = "r@r.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = targetId, Email = "t@t.com", PasswordHash = "h" });

            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = requesterId, Role = ProjectRole.Member });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = targetId, Role = ProjectRole.Member });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteByIdAsync(targetId, projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Only administrators can remove other members from the project.");
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldReturnNotFound_WhenTargetMemberDoesNotExist()
        {
            var requesterId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(requesterId);

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Users.Add(new User { Id = requesterId, Email = "r@r.com", PasswordHash = "h" });

            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = requesterId, Role = ProjectRole.Admin });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteByIdAsync(targetId, projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project member was not found.");
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldReturnConflict_WhenDeletingLastAdmin()
        {
            var adminId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(adminId);

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Users.Add(new User { Id = adminId, Email = "r@r.com", PasswordHash = "h" });

            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = adminId, Role = ProjectRole.Admin });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteByIdAsync(adminId, projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Can't remove the last administrator of the project.");
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldRemoveMemberAndUnassignTasks_WhenSuccessful()
        {
            var requesterId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(requesterId);

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Users.Add(new User { Id = requesterId, Email = "r@r.com", PasswordHash = "h" });
            _context.Users.Add(new User { Id = targetId, Email = "t@t.com", PasswordHash = "h" });

            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = requesterId, Role = ProjectRole.Admin });
            _context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = targetId, Role = ProjectRole.Member });

            _context.TaskItems.Add(new TaskItem { Id = taskId, ProjectId = projectId, UserId = targetId, Title = "Task" });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteByIdAsync(targetId, projectId, CancellationToken.None);

            _context.ChangeTracker.Clear();

            result.IsSuccess.Should().BeTrue();

            var dbMember = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == targetId && pm.ProjectId == projectId);
            dbMember.Should().BeNull();

            var dbTask = await _context.TaskItems.FindAsync(taskId);
            dbTask!.UserId.Should().BeNull();
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldRollbackAndThrow_WhenExceptionOccurs()
        {
            var adminId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(adminId);

            using var faultyContext = new FaultyDbContext(_options);
            faultyContext.Database.EnsureCreated();

            faultyContext.Projects.Add(new Project { Id = projectId, Name = "P" });
            faultyContext.Users.Add(new User { Id = adminId, Email = "a@a.com", PasswordHash = "h" });
            faultyContext.Users.Add(new User { Id = targetId, Email = "t@t.com", PasswordHash = "h" });
            faultyContext.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = adminId, Role = ProjectRole.Admin });
            faultyContext.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = targetId, Role = ProjectRole.Member });
            await faultyContext.SaveChangesAsync();

            var serviceWithFaultyDb = new ProjectMemberService(
                faultyContext, _mapper, _userContextServiceMock.Object, _projectAccessServiceMock.Object, _loggerMock.Object);

            Func<Task> act = async () => await serviceWithFaultyDb.DeleteByIdAsync(targetId, projectId, CancellationToken.None);

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