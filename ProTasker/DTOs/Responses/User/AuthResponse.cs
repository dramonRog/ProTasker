namespace ProTasker.DTOs.Responses.User
{
    public record AuthResponse(string Token, string RefreshToken, DateTime ExpiresAt, UserResponse User);
}
