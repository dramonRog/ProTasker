namespace ProTasker.DTOs.Requests.User
{
    public record UpdateUserRequest(string? FirstName, string? LastName, string? Email, string? Password);
}
