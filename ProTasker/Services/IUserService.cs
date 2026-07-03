using ProTasker.Common;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;

namespace ProTasker.Services
{
    public interface IUserService
    {
        Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken);
        Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<Result<UserResponse>> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
