namespace ProTasker.DTOs.Requests.User
{
    public record RegisterUserRequest(string FirstName, string LastName, string Email, string Password);
}
