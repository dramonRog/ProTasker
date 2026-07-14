using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.Models.Enums;
using ProTasker.Validators.TaskItem;
using Xunit;

namespace ProTasker.UnitTests.Validators.TaskItem
{
    public class ChangeTaskPriorityRequestValidatorTests
    {
        private readonly ChangeTaskPriorityRequestValidator _validator;

        public ChangeTaskPriorityRequestValidatorTests()
        {
            _validator = new ChangeTaskPriorityRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_PriorityIsValid()
        {
            var request = new ChangeTaskPriorityRequest(TaskPriority.High);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Priority);
        }

        [Fact]
        public void Should_HaveError_When_PriorityIsInvalid()
        {
            var request = new ChangeTaskPriorityRequest((TaskPriority)999);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Priority)
                  .WithErrorMessage("Invalid priority level.");
        }
    }
}