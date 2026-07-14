using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.Validators.TaskItem;
using Xunit;

namespace ProTasker.UnitTests.Validators.TaskItem
{
    public class AssignTaskRequestValidatorTests
    {
        private readonly AssignTaskRequestValidator _validator;

        public AssignTaskRequestValidatorTests()
        {
            _validator = new AssignTaskRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_UserIdIsValid()
        {
            var request = new AssignTaskRequest(Guid.NewGuid());
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Should_HaveError_When_UserIdIsEmpty()
        {
            var request = new AssignTaskRequest(Guid.Empty);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.UserId)
                  .WithErrorMessage("Assigned user is required.");
        }

        [Fact]
        public void Should_HaveError_When_UserIdIsNull()
        {
            var request = new AssignTaskRequest(null);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.UserId)
                  .WithErrorMessage("Assigned user is required.");
        }
    }
}