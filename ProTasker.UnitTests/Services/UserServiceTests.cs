using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Data;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Models;
using ProTasker.Models.Entities;
using ProTasker.Models.Enums;
using ProTasker.Pagination;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly Mock<IUserContextService> _userContextServiceMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _userContextServiceMock = new Mock<IUserContextService>();
            _loggerMock = new Mock<ILogger<UserService>>();

            var mapperConfig = new AutoMapper.MapperConfiguration(
                cfg =>
                {
                    cfg.CreateMap<User, UserResponse>();
                },
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
            );

            _mapper = mapperConfig.CreateMapper();

            _userService = new UserService(_mapper, _userContextServiceMock.Object, _context, _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnOnlyCurrentUserAndUsersInSharedProjects()
        {
            var currentUserId = Guid.NewGuid();
            var sharedUserId = Guid.NewGuid();
            var isolatedUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);

            _context.Users.AddRange(
                new User { Id = currentUserId, FirstName = "Current", Email = "1@test.com", PasswordHash = "hash" },
                new User { Id = sharedUserId, FirstName = "Shared", Email = "2@test.com", PasswordHash = "hash" },
                new User { Id = isolatedUserId, FirstName = "Isolated", Email = "3@test.com", PasswordHash = "hash" }
            );

            _context.Projects.Add(new Project { Id = projectId, Name = "Shared Project" });
            _context.ProjectMembers.AddRange(
                new ProjectMember { UserId = currentUserId, ProjectId = projectId, Role = ProjectRole.Member },
                new ProjectMember { UserId = sharedUserId, ProjectId = projectId, Role = ProjectRole.Member }
            );

            await _context.SaveChangesAsync();
            var pagination = new PaginationQuery { PageNumber = 1, PageSize = 10 };

            var result = await _userService.GetAllAsync(pagination, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(2);
            result.Value.Items.Should().Contain(u => u.Id == currentUserId);
            result.Value.Items.Should().Contain(u => u.Id == sharedUserId);
            result.Value.Items.Should().NotContain(u => u.Id == isolatedUserId);
        }

        [Fact]
        public async Task GetAllAsync_ShouldApplyPaginationCorrectly()
        {
            var currentUserId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);

            var projectId = Guid.NewGuid();
            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            _context.Users.Add(new User { Id = currentUserId, Email = "current@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
            _context.ProjectMembers.Add(new ProjectMember { UserId = currentUserId, ProjectId = projectId, Role = ProjectRole.Member });

            for (int i = 0; i < 5; i++)
            {
                var userId = Guid.NewGuid();
                _context.Users.Add(new User { Id = userId, Email = $"user{i}@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow.AddMinutes(i + 1) });
                _context.ProjectMembers.Add(new ProjectMember { UserId = userId, ProjectId = projectId, Role = ProjectRole.Member });
            }
            await _context.SaveChangesAsync();

            var pagination = new PaginationQuery { PageNumber = 2, PageSize = 2 };

            var result = await _userService.GetAllAsync(pagination, CancellationToken.None);

            result.Value!.TotalCount.Should().Be(6);
            result.Value.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WhenRequestingOwnProfile()
        {
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);
            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var result = await _userService.GetByIdAsync(userId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Id.Should().Be(userId);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenRequestingUserOutsideSharedProjects()
        {
            var currentUserId = Guid.NewGuid();
            var isolatedUserId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);

            _context.Users.Add(new User { Id = currentUserId, Email = "1@test.com", PasswordHash = "hash" });
            _context.Users.Add(new User { Id = isolatedUserId, Email = "2@test.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var result = await _userService.GetByIdAsync(isolatedUserId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User was not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
            _context.Users.Add(new User { Id = currentUserId, Email = "test@test.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var result = await _userService.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User was not found.");
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldUpdateFields_WhenProvidedInRequest()
        {
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);
            var user = new User { Id = userId, FirstName = "Old", LastName = "Old", Email = "old@test.com", PasswordHash = "oldHash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new UpdateUserRequest("NewFirst", null, "NEW@test.com", "NewPassword123!");

            var result = await _userService.UpdateUserAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dbUser = await _context.Users.FindAsync(userId);
            dbUser!.FirstName.Should().Be("NewFirst");
            dbUser.LastName.Should().Be("Old");
            dbUser.Email.Should().Be("new@test.com");
            BCrypt.Net.BCrypt.Verify("NewPassword123!", dbUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());
            var request = new UpdateUserRequest("First", "Last", "test@test.com", "Password123!");

            var result = await _userService.UpdateUserAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User was not found.");
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnConflict_WhenEmailIsAlreadyTakenByAnotherUser()
        {
            var currentUserId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);

            _context.Users.Add(new User { Id = currentUserId, Email = "my@test.com", PasswordHash = "hash" });
            _context.Users.Add(new User { Id = Guid.NewGuid(), Email = "taken@test.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var request = new UpdateUserRequest(null, null, "TAKEN@test.com", null);

            var result = await _userService.UpdateUserAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Email is already in use.");
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnConflict_OnDbUpdateException_RaceCondition()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var currentUserId = Guid.NewGuid();

            using (var setupContext = new AppDbContext(options))
            {
                setupContext.Users.Add(new User { Id = currentUserId, Email = "old@test.com", PasswordHash = "hash" });
                await setupContext.SaveChangesAsync();
            }

            using var faultyContext = new FaultyDbContext(options);
            var serviceWithFaultyDb = new UserService(_mapper, _userContextServiceMock.Object, faultyContext, _loggerMock.Object);
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);

            var request = new UpdateUserRequest(null, null, "new@test.com", null);

            var result = await serviceWithFaultyDb.UpdateUserAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Email is already in use.");
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldUpdateAllFields_WhenAllAreProvidedInRequest()
        {
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);
            var user = new User { Id = userId, FirstName = "Old", LastName = "Old", Email = "old@test.com", PasswordHash = "oldHash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new UpdateUserRequest("NewFirst", "NewLast", "NEW@test.com", "NewPassword123!");

            var result = await _userService.UpdateUserAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dbUser = await _context.Users.FindAsync(userId);
            dbUser!.FirstName.Should().Be("NewFirst");
            dbUser.LastName.Should().Be("NewLast");
            dbUser.Email.Should().Be("new@test.com");
            BCrypt.Net.BCrypt.Verify("NewPassword123!", dbUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldSkipEmailAndOtherFields_WhenOnlyLastNameIsProvided()
        {
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);
            var user = new User { Id = userId, FirstName = "OldFirst", LastName = "OldLast", Email = "old@test.com", PasswordHash = "oldHash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new UpdateUserRequest(null, "NewLast", null, null);

            var result = await _userService.UpdateUserAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dbUser = await _context.Users.FindAsync(userId);
            dbUser!.FirstName.Should().Be("OldFirst");
            dbUser.LastName.Should().Be("NewLast");
            dbUser.Email.Should().Be("old@test.com");
            dbUser.PasswordHash.Should().Be("oldHash");
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldDeleteUser_WhenUserIsNotSoleAdminOfAnyProject()
        {
            var userId = Guid.NewGuid();
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);
            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            await _context.SaveChangesAsync();

            var result = await _userService.DeleteUserAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _context.Users.Count().Should().Be(0);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldDeleteUser_WhenUserIsAdminButAnotherAdminExists()
        {
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);
            _context.Users.Add(new User { Id = userId, Email = "1@test.com", PasswordHash = "hash" });
            _context.Users.Add(new User { Id = otherUserId, Email = "2@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });

            _context.ProjectMembers.AddRange(
                new ProjectMember { UserId = userId, ProjectId = projectId, Role = ProjectRole.Admin },
                new ProjectMember { UserId = otherUserId, ProjectId = projectId, Role = ProjectRole.Admin }
            );
            await _context.SaveChangesAsync();

            var result = await _userService.DeleteUserAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _context.Users.Count().Should().Be(1);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldReturnConflict_WhenUserIsSoleAdminOfProject()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(userId);
            _context.Users.Add(new User { Id = userId, Email = "test@test.com", PasswordHash = "hash" });
            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });

            _context.ProjectMembers.Add(new ProjectMember { UserId = userId, ProjectId = projectId, Role = ProjectRole.Admin });
            await _context.SaveChangesAsync();

            var result = await _userService.DeleteUserAsync(CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("There is at least one project, where only you are an admin. You must remove it or assign another user as the admin.");
            _context.Users.Count().Should().Be(1);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

            var result = await _userService.DeleteUserAsync(CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User was not found.");
        }

        private class FaultyDbContext : AppDbContext
        {
            public FaultyDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (ChangeTracker.HasChanges())
                    throw new DbUpdateException("Simulated race condition");

                return base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}