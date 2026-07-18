using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProTasker.Common;
using ProTasker.Data;
using ProTasker.DTOs.Requests.Board;
using ProTasker.Mapping;
using ProTasker.Models;
using ProTasker.Services.Implementations;
using ProTasker.Services.Interfaces;
using Xunit;

namespace ProTasker.UnitTests.Services
{
    public class BoardServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<IUserContextService> _userContextServiceMock;
        private readonly Mock<IProjectAccessService> _projectAccessServiceMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<BoardService>> _loggerMock;
        private readonly BoardService _boardService;

        public BoardServiceTests()
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
            _loggerMock = new Mock<ILogger<BoardService>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<BoardMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _boardService = new BoardService(
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

        #region GetAllByProjectAsync Tests

        [Fact]
        public async Task GetAllByProjectAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var result = await _boardService.GetAllByProjectAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task GetAllByProjectAsync_ShouldReturnBoardsOrderedByIndex_WhenSuccessful()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            _context.Projects.Add(new Project { Id = projectId, Name = "Test Project" });

            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "Done", OrderIndex = 2 });
            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "To Do", OrderIndex = 0 });
            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "In Progress", OrderIndex = 1 });
            await _context.SaveChangesAsync();

            var result = await _boardService.GetAllByProjectAsync(projectId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(3);
            result.Value![0].Name.Should().Be("To Do");
            result.Value[1].Name.Should().Be("In Progress");
            result.Value[2].Name.Should().Be("Done");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenBoardDoesNotExist()
        {
            var result = await _boardService.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Board was not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            var boardId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "Board" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Forbidden("Access denied"));

            var result = await _boardService.GetByIdAsync(boardId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Access denied");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBoard_WhenSuccessful()
        {
            var boardId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "My Board" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureMemberAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            var result = await _boardService.GetByIdAsync(boardId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Name.Should().Be("My Board");
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var request = new CreateBoardRequest("New", 0, "#FFF");
            var result = await _boardService.CreateAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnConflict_WhenOrderIndexIsTaken()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "Existing", OrderIndex = 1 });
            await _context.SaveChangesAsync();

            var request = new CreateBoardRequest("New", 1, "#FFF");
            var result = await _boardService.CreateAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("A board with this order index already exists in the project.");
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateBoard_WhenDataIsValid()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            await _context.SaveChangesAsync();

            var request = new CreateBoardRequest("New Board", 0, "#FFF");
            var result = await _boardService.CreateAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Name.Should().Be("New Board");

            var dbBoard = await _context.Boards.FirstAsync(b => b.ProjectId == projectId);
            dbBoard.Name.Should().Be("New Board");
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldReturnNotFound_WhenBoardDoesNotExist()
        {
            var request = new UpdateBoardRequest("Name", 1, "#000");
            var result = await _boardService.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Board was not found.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            var boardId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B", OrderIndex = 0 });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var request = new UpdateBoardRequest("Name", 1, "#000");
            var result = await _boardService.UpdateAsync(boardId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnConflict_WhenNewOrderIndexIsTaken()
        {
            var board1Id = Guid.NewGuid();
            var board2Id = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            _context.Boards.Add(new Board { Id = board1Id, ProjectId = projectId, Name = "B1", OrderIndex = 0 });
            _context.Boards.Add(new Board { Id = board2Id, ProjectId = projectId, Name = "B2", OrderIndex = 1 });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var request = new UpdateBoardRequest(null, 1, null);
            var result = await _boardService.UpdateAsync(board1Id, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("A board with this order index already exists in the project.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateProvidedFields_AndSkipNulls()
        {
            var boardId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "Test" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "Old", OrderIndex = 0, Color = "#000" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var request = new UpdateBoardRequest("New", 5, null);
            var result = await _boardService.UpdateAsync(boardId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            var dbBoard = await _context.Boards.FindAsync(boardId);
            dbBoard!.Name.Should().Be("New");
            dbBoard.OrderIndex.Should().Be(5);
            dbBoard.Color.Should().Be("#000");
        }

        #endregion

        #region ReorderAsync Tests

        [Fact]
        public async Task ReorderAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var request = new ReorderBoardsRequest(new List<Guid>());
            var result = await _boardService.ReorderAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task ReorderAsync_ShouldReturnValidation_WhenBoardCountsMismatch()
        {
            var projectId = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "B1", OrderIndex = 0 });
            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "B2", OrderIndex = 1 });
            await _context.SaveChangesAsync();

            var request = new ReorderBoardsRequest(new List<Guid> { Guid.NewGuid() }); 
            var result = await _boardService.ReorderAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("The provided board IDs are invalid or do not match the existing boards in the project.");
        }

        [Fact]
        public async Task ReorderAsync_ShouldReorderBoards_WhenSuccessful()
        {
            var projectId = Guid.NewGuid();
            var b1 = Guid.NewGuid();
            var b2 = Guid.NewGuid();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = b1, ProjectId = projectId, Name = "B1", OrderIndex = 0 });
            _context.Boards.Add(new Board { Id = b2, ProjectId = projectId, Name = "B2", OrderIndex = 1 });
            await _context.SaveChangesAsync();

            var request = new ReorderBoardsRequest(new List<Guid> { b2, b1 });
            var result = await _boardService.ReorderAsync(projectId, request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            var dbBoards = await _context.Boards.Where(b => b.ProjectId == projectId).ToListAsync();
            dbBoards.First(b => b.Id == b2).OrderIndex.Should().Be(0);
            dbBoards.First(b => b.Id == b1).OrderIndex.Should().Be(1);
        }

        [Fact]
        public async Task ReorderAsync_ShouldRollbackAndThrow_OnException()
        {
            var projectId = Guid.NewGuid();
            var b1 = Guid.NewGuid();
            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            using var faultyContext = new FaultyDbContext(_options);
            faultyContext.Database.EnsureCreated();
            faultyContext.Projects.Add(new Project { Id = projectId, Name = "P" });
            faultyContext.Boards.Add(new Board { Id = b1, ProjectId = projectId, Name = "B1", OrderIndex = 0 });
            await faultyContext.SaveChangesAsync(); 

            var faultyService = new BoardService(faultyContext, _userContextServiceMock.Object, _projectAccessServiceMock.Object, _mapper, _loggerMock.Object);

            var request = new ReorderBoardsRequest(new List<Guid> { b1 });
            Func<Task> act = async () => await faultyService.ReorderAsync(projectId, request, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>().WithMessage("Simulated DB failure");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldReturnNotFound_WhenBoardDoesNotExist()
        {
            var result = await _boardService.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Board was not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            var boardId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "B1" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Forbidden("Not admin"));

            var result = await _boardService.DeleteAsync(boardId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Not admin");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnConflict_WhenDeletingLastBoardInProject()
        {
            var boardId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardId, ProjectId = projectId, Name = "Only Board" });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var result = await _boardService.DeleteAsync(boardId, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Cannot delete the last board in a project.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteBoard_WhenMultipleBoardsExist()
        {
            var boardIdToDelete = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            _context.Projects.Add(new Project { Id = projectId, Name = "P" });
            _context.Boards.Add(new Board { Id = boardIdToDelete, ProjectId = projectId, Name = "Board 1", OrderIndex = 0 });
            _context.Boards.Add(new Board { Id = Guid.NewGuid(), ProjectId = projectId, Name = "Board 2", OrderIndex = 1 });
            await _context.SaveChangesAsync();

            _projectAccessServiceMock.Setup(x => x.EnsureAdminAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectMember>.Success(new ProjectMember()));

            var result = await _boardService.DeleteAsync(boardIdToDelete, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            var remainingBoards = await _context.Boards.Where(b => b.ProjectId == projectId).ToListAsync();
            remainingBoards.Should().HaveCount(1);
            remainingBoards.First().Id.Should().NotBe(boardIdToDelete);
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