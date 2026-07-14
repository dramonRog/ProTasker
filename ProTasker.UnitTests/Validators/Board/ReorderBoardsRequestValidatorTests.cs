using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.Board;
using ProTasker.Validators.Board;
using Xunit;

namespace ProTasker.UnitTests.Validators.Board
{
    public class ReorderBoardsRequestValidatorTests
    {
        private readonly ReorderBoardsRequestValidator _validator;

        public ReorderBoardsRequestValidatorTests()
        {
            _validator = new ReorderBoardsRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_BoardIdsAreValidAndDistinct()
        {
            var request = new ReorderBoardsRequest(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.BoardIds);
        }

        [Fact]
        public void Should_HaveError_When_BoardIdsListIsEmpty()
        {
            var request = new ReorderBoardsRequest(new List<Guid>());
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.BoardIds)
                  .WithErrorMessage("The board IDs list cannot be empty.");
        }

        [Fact]
        public void Should_HaveError_When_BoardIdsListIsNull()
        {
            var request = new ReorderBoardsRequest(null!);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.BoardIds)
                  .WithErrorMessage("The board IDs list cannot be empty.");
        }

        [Fact]
        public void Should_HaveError_When_BoardIdsListContainsDuplicates()
        {
            var duplicateId = Guid.NewGuid();
            var request = new ReorderBoardsRequest(new List<Guid> { duplicateId, duplicateId });

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.BoardIds)
                  .WithErrorMessage("The board IDs list contains duplicates.");
        }
    }
}