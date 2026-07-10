namespace ProTasker.DTOs.Responses.Board
{
    public record BoardSummaryResponse(Guid Id, string Name, int OrderIndex, string? Color);
}
