using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskItem>();

        task.HasKey(t => t.Id);
        task.Property(t => t.Title).IsRequired().HasMaxLength(200);
        task.Property(t => t.Description).HasMaxLength(2000);

        base.OnModelCreating(modelBuilder);
    }
}
