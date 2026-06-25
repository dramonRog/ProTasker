namespace ProTasker.Models
{
    public class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;

        public required Project Project { get; set; }
        public required User User { get; set; }
    }
}
