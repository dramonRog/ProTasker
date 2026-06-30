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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(pm => new { pm.ProjectId, pm.UserId });

                entity.HasOne(pm => pm.Project)
                    .WithMany(p => p.ProjectMembers)
                    .HasForeignKey(pm => pm.ProjectId);

                entity.HasOne(pm => pm.User)
                    .WithMany(u => u.ProjectMembers)
                    .HasForeignKey(pm => pm.UserId);
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasOne(ti => ti.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(ti => ti.ProjectId);

                entity.HasOne(ti => ti.User)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(ti => ti.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(ti => ti.Status)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();


        }
    }
}
