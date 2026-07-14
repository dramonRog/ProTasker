using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.Validators.ProjectMember;
using Xunit;
using System;

namespace ProTasker.UnitTests.Validators.ProjectMember
{
    public class AddProjectMemberRequestValidatorTests
    {
        private readonly AddProjectMemberRequestValidator _validator;

        public AddProjectMemberRequestValidatorTests()
        {
            _validator = new AddProjectMemberRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_UserIdIsValid()
        {
            var request = new AddProjectMemberRequest(Guid.NewGuid());
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Should_HaveError_When_UserIdIsEmpty()
        {
            var request = new AddProjectMemberRequest(Guid.Empty);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.UserId)
                  .WithErrorMessage("Select a user to add to this project.");
        }
    }
}