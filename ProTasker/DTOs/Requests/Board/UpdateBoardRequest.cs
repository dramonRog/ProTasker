namespace ProTasker.DTOs.Requests.Board
{
    public record UpdateBoardRequest(string? Name, int? OrderIndex, string? Color);
}
