using ProTasker.Models;

namespace ProTasker.Services
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) CreateToken(User user);
    }
}
