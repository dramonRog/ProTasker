namespace ProTasker.DTOs.Responses.Board
{
    public record BoardResponse(Guid Id, Guid ProjectId, string Name, int OrderIndex, string? Color, DateTime CreatedAt);
}
