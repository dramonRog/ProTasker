using Microsoft.AspNetCore.Mvc;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Services;

namespace ProTasker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthResponse>> RegisterUser(RegisterUserRequest request, CancellationToken cancellationToken)
        {
            AuthResponse? response = await _authService.RegisterAsync(request, cancellationToken);

            if (response == null)
                return BadRequest("Email is already taken.");

            return Ok(response);
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthResponse>> LoginUser(LoginUserRequest request, CancellationToken cancellationToken)
        {
            AuthResponse? response = await _authService.LoginAsync(request, cancellationToken);

            if (response == null)
                return Unauthorized("Invalid email or password.");

            return Ok(response);
        }
    }
}
