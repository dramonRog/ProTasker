namespace ProTasker.Models
{
    public class Board
    {
        public Guid Id { get; set;  }
        public int OrderIndex { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Color { get; set; }
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
