using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;

namespace ProTasker.Services
{
    public interface IUserService
    {
        Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<UserResponse?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
