namespace ProTasker.DTOs.Responses.User
{
    public record UserResponse(Guid Id, string FirstName, string LastName, string Email, DateTime CreatedAt);
}
