using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProTasker.Common;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Services.Interfaces;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserResponse>>> GetAllUsers(CancellationToken cancellationToken)
        {
            var result = await _userService.GetAllAsync(cancellationToken);
            return result.CastToResultCode();
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponse>> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _userService.GetByIdAsync(id, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpPatch("me")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponse>> UpdateUser(UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.UpdateUserAsync(request, cancellationToken);
            return result.CastToResultCode();
        }

        [HttpDelete("me")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteUser(CancellationToken cancellationToken)
        {
            var result = await _userService.DeleteUserAsync(cancellationToken);
            return result.CastToResultCode();
        }
    }
}
