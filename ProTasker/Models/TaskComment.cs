namespace ProTasker.Models
{
    public class TaskComment : ISoftDeletable
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? UserId { get; set; }
        public Guid TaskId { get; set; }
        public User? User { get; set; }
        public TaskItem? Task { get; set; }
    }
}
