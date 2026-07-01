using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;

namespace ProTasker.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken);
        Task<AuthResponse?> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken);
    }
}
