namespace ProTasker.DTOs.Requests.Board
{
    public record CreateBoardRequest(string Name, int OrderIndex, string? Color = null);
}
