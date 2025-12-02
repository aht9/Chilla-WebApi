using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; } 

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Apply Global Query Filter for Soft Delete
        builder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        // Concurrency Token
        builder.Entity<User>()
            .Property(u => u.RowVersion)
            .IsRowVersion();

        base.OnModelCreating(builder);
    }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Content { get; set; } // JSON Payload
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? Error { get; set; }
}