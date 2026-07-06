namespace ProTasker.Models
{
    public class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public ProjectRole Role { get; set; } = ProjectRole.Member;
        public required Project Project { get; set; }
        public required User User { get; set; }
    }
}
