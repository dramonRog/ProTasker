namespace ProTasker.DTOs.Responses.User
{
    public record AuthResponse(string Token, DateTime ExpiresAt, UserResponse User);
}
