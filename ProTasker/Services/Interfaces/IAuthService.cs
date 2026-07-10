using ProTasker.Common;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;

namespace ProTasker.Services.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken);
        Task<Result<AuthResponse>> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken);
    }
}
