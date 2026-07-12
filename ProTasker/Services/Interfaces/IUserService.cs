using ProTasker.Common;
using ProTasker.DTOs.Requests.User;
using ProTasker.DTOs.Responses.User;
using ProTasker.Pagination;

namespace ProTasker.Services.Interfaces
{
    public interface IUserService
    {
        Task<Result<PagedResult<UserResponse>>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken);
        Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<Result<UserResponse>> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken);
        Task<Result> DeleteUserAsync(CancellationToken cancellationToken);
    }
}
