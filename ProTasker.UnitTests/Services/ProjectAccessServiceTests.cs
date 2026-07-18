using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Data;
using ProTasker.Models;
using ProTasker.Models.Enums;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class ProjectAccessServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IUserContextService> _userContextServiceMock;
        private readonly Mock<ILogger<ProjectAccessService>> _loggerMock;
        private readonly ProjectAccessService _projectAccessService;

        public ProjectAccessServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _userContextServiceMock = new Mock<IUserContextService>();
            _loggerMock = new Mock<ILogger<ProjectAccessService>>();

            _projectAccessService = new ProjectAccessService(
                _context,
                _userContextServiceMock.Object,
                _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region EnsureMemberAsync Tests

        [Fact]
        public async Task EnsureMemberAsync_ShouldReturnSuccess_WhenUserIsMemberOfProject()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "Test Project" });
            _context.ProjectMembers.Add(new ProjectMember { UserId = userId, ProjectId = projectId, Role = ProjectRole.Member });
            await _context.SaveChangesAsync();

            var result = await _projectAccessService.EnsureMemberAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task EnsureMemberAsync_ShouldReturnForbidden_WhenUserIsNotMemberOrProjectDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            var result = await _projectAccessService.EnsureMemberAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You are not a member of this project, or it has been deleted.");
        }

        #endregion

        #region EnsureAdminAsync Tests

        [Fact]
        public async Task EnsureAdminAsync_ShouldReturnForbidden_WhenUserIsNotMemberOrProjectDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            var result = await _projectAccessService.EnsureAdminAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You are not a member of this project, or it has been deleted.");
        }

        [Fact]
        public async Task EnsureAdminAsync_ShouldReturnForbidden_WhenUserIsMemberButNotAdmin()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "Test Project" });

            _context.ProjectMembers.Add(new ProjectMember { UserId = userId, ProjectId = projectId, Role = ProjectRole.Member });
            await _context.SaveChangesAsync();

            var result = await _projectAccessService.EnsureAdminAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Only administrators can perform this action.");
        }

        [Fact]
        public async Task EnsureAdminAsync_ShouldReturnSuccessWithAdminMember_WhenUserIsAdmin()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "Test Project" });

            _context.ProjectMembers.Add(new ProjectMember { UserId = userId, ProjectId = projectId, Role = ProjectRole.Admin });
            await _context.SaveChangesAsync();

            var result = await _projectAccessService.EnsureAdminAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Role.Should().Be(ProjectRole.Admin);
            result.Value.UserId.Should().Be(userId);
            result.Value.ProjectId.Should().Be(projectId);
        }

        #endregion
    }
}