using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.Models.Enums;
using ProTasker.Validators.TaskItem;
using Xunit;

namespace ProTasker.UnitTests.Validators.TaskItem
{
    public class UpdateTaskItemRequestValidatorTests
    {
        private readonly UpdateTaskItemRequestValidator _validator;

        public UpdateTaskItemRequestValidatorTests()
        {
            _validator = new UpdateTaskItemRequestValidator();
        }

        [Fact]
        public void Should_HaveErrorForObject_When_AllFieldsAreNull()
        {
            var request = new UpdateTaskItemRequest(null, null, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Submit at least one field to update.");
        }

        [Fact]
        public void Should_NotHaveError_When_AtLeastOneFieldIsProvided()
        {
            var request = new UpdateTaskItemRequest("Updated Title", null, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x);
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Should_HaveError_When_TitleIsProvidedButEmpty()
        {
            var request = new UpdateTaskItemRequest("", null, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Task title can't be empty.");
        }

        [Fact]
        public void Should_HaveError_When_DescriptionIsTooLong()
        {
            var request = new UpdateTaskItemRequest(null, new string('a', 4001), null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("The maximum length is 4000 characters.");
        }

        [Fact]
        public void Should_HaveError_When_DueDateIsInThePast()
        {
            var request = new UpdateTaskItemRequest(null, null, null, DateTime.UtcNow.AddDays(-1));
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.DueDate)
                  .WithErrorMessage("Due date must be in the future.");
        }

        [Fact]
        public void Should_HaveError_When_PriorityIsInvalid()
        {
            var request = new UpdateTaskItemRequest(null, null, (TaskPriority)999, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Priority)
                  .WithErrorMessage("Invalid priority level.");
        }

        [Fact]
        public void Should_HaveError_When_TitleIsTooLong()
        {
            var request = new UpdateTaskItemRequest(new string('a', 301), null, null, null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("The maximum length is 300 characters.");
        }
    }
}