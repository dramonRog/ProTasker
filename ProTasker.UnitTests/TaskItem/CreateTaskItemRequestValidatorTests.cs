using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.Models.Enums;
using ProTasker.Validators.TaskItem;
using Xunit;
using System;

namespace ProTasker.UnitTests.Validators.TaskItem
{
    public class CreateTaskItemRequestValidatorTests
    {
        private readonly CreateTaskItemRequestValidator _validator;

        public CreateTaskItemRequestValidatorTests()
        {
            _validator = new CreateTaskItemRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_ModelIsValid()
        {
            var request = new CreateTaskItemRequest(
                Title: "Valid Title",
                Description: "Valid Description",
                DueDate: DateTime.UtcNow.AddDays(2),
                ProjectId: Guid.NewGuid(),
                Priority: TaskPriority.Medium,
                BoardId: Guid.NewGuid()
            );

            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_HaveError_When_ProjectIdIsEmpty()
        {
            var request = new CreateTaskItemRequest(
                Title: "Title",
                Description: null,
                DueDate: null,
                ProjectId: Guid.Empty,
                Priority: TaskPriority.Low
            );

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.ProjectId)
                  .WithErrorMessage("Target project is required.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_HaveError_When_TitleIsEmpty(string? invalidTitle)
        {
            var request = new CreateTaskItemRequest(
                Title: invalidTitle!,
                Description: null,
                DueDate: null,
                ProjectId: Guid.NewGuid(),
                Priority: TaskPriority.Low
            );

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Task title is required.");
        }

        [Fact]
        public void Should_HaveError_When_TitleIsTooLong()
        {
            var request = new CreateTaskItemRequest(
                Title: new string('a', 301),
                Description: null,
                DueDate: null,
                ProjectId: Guid.NewGuid(),
                Priority: TaskPriority.Low
            );

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("The maximum length is 300 characters.");
        }

        [Fact]
        public void Should_HaveError_When_DescriptionIsTooLong()
        {
            var request = new CreateTaskItemRequest(
                Title: "Title",
                Description: new string('a', 4001),
                DueDate: null,
                ProjectId: Guid.NewGuid(),
                Priority: TaskPriority.Low
            );

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("The maximum length is 4000 characters.");
        }

        [Fact]
        public void Should_HaveError_When_DueDateIsInThePast()
        {
            var request = new CreateTaskItemRequest(
                Title: "Title",
                Description: null,
                DueDate: DateTime.UtcNow.AddDays(-1),
                ProjectId: Guid.NewGuid(),
                Priority: TaskPriority.Low
            );

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.DueDate)
                  .WithErrorMessage("The due date must be in the future.");
        }

        [Fact]
        public void Should_HaveError_When_BoardIdIsEmptyAndHasValue()
        {
            var request = new CreateTaskItemRequest(
                Title: "Title",
                Description: null,
                DueDate: null,
                ProjectId: Guid.NewGuid(),
                Priority: TaskPriority.Low,
                BoardId: Guid.Empty
            );

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.BoardId)
                  .WithErrorMessage("Board id cannot be empty.");
        }

        [Fact]
        public void Should_HaveError_When_PriorityIsInvalid()
        {
            var request = new CreateTaskItemRequest(
                Title: "Title",
                Description: null,
                DueDate: null,
                ProjectId: Guid.NewGuid(),
                Priority: (TaskPriority)999, 
                BoardId: null
            );

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Priority)
                  .WithErrorMessage("Invalid priority level.");
        }
    }
}