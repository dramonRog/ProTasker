using FluentValidation.TestHelper;
using ProTasker.DTOs.Requests.ProjectMember;
using ProTasker.Models.Enums;
using ProTasker.Validators.ProjectMember;
using Xunit;

namespace ProTasker.UnitTests.Validators.ProjectMember
{
    public class ChangeProjectMemberRoleRequestValidatorTests
    {
        private readonly ChangeProjectMemberRoleRequestValidator _validator;

        public ChangeProjectMemberRoleRequestValidatorTests()
        {
            _validator = new ChangeProjectMemberRoleRequestValidator();
        }

        [Fact]
        public void Should_NotHaveError_When_RoleIsValid()
        {
            var request = new ChangeProjectMemberRole((ProjectRole)1);
            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Role);
        }

        [Fact]
        public void Should_HaveError_When_RoleIsInvalid()
        {
            var request = new ChangeProjectMemberRole((ProjectRole)999);
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Role)
                  .WithErrorMessage("Invalid role specified.");
        }
    }
}