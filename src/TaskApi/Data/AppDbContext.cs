using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Data;

public sealed class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configures the Identity tables (AspNetUsers, AspNetRoles, ...).
        base.OnModelCreating(modelBuilder);

        var task = modelBuilder.Entity<TaskItem>();

        task.HasKey(t => t.Id);
        task.Property(t => t.Title).IsRequired().HasMaxLength(200);
        task.Property(t => t.Description).HasMaxLength(2000);

        // SQLite stores DateTime as text without a kind; mark values read back as UTC.
        task.Property(t => t.CreatedAt)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        task.Property(t => t.UpdatedAt)
            .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        task.HasOne(t => t.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        task.HasIndex(t => new { t.UserId, t.IsCompleted });
    }
}
