namespace ProTasker.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? BoardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public Project? Project { get; set; }
        public User? User { get; set; }
        public Board? Board { get; set; }
    }
}
