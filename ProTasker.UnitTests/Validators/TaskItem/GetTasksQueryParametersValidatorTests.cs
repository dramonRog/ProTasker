using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.Models.Enums;
using ProTasker.Validators.TaskItem;
using Xunit;

namespace ProTasker.UnitTests.Validators.TaskItem
{
    public class GetTasksQueryParametersValidatorTests
    {
        private readonly GetTasksQueryParametersValidator _validator;

        public GetTasksQueryParametersValidatorTests()
        {
            _validator = new GetTasksQueryParametersValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_ModelIsEmpty()
        {
            var request = new GetTasksQueryParameters(null, null, null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_HaveError_When_PriorityIsInvalid()
        {
            var request = new GetTasksQueryParameters((TaskPriority)999, null, null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Priority)
                  .WithErrorMessage("Invalid priority was specified.");
        }

        [Fact]
        public void Should_HaveError_When_SearchTermIsTooLong()
        {
            var request = new GetTasksQueryParameters(null, null, new string('a', 101));
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
                  .WithErrorMessage("Search term is too long (max 100 characters).");
        }
    }
}