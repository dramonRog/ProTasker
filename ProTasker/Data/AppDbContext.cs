using Microsoft.EntityFrameworkCore;
using ProTasker.Models;

namespace ProTasker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Board> Boards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(pm => new { pm.ProjectId, pm.UserId });

                entity.HasOne(pm => pm.Project)
                    .WithMany(p => p.ProjectMembers)
                    .HasForeignKey(pm => pm.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pm => pm.User)
                    .WithMany(u => u.ProjectMembers)
                    .HasForeignKey(pm => pm.UserId);
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasOne(ti => ti.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(ti => ti.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ti => ti.User)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(ti => ti.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(ti => ti.Board)
                    .WithMany(b => b.Tasks)
                    .HasForeignKey(ti => ti.BoardId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(ti => ti.Priority)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Board>(entity =>
            {
                entity.HasOne(b => b.Project)
                .WithMany(p => p.Boards)
                .HasForeignKey(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(b => new { b.ProjectId, b.OrderIndex }).IsUnique();

            });

            modelBuilder.Entity<TaskItem>().HasQueryFilter(ti => !ti.IsDeleted);
            modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
