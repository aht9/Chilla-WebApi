using System.Reflection;
using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.RoleAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;

namespace Chilla.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    // Aggregate Roots & Entities
    public DbSet<User> Users { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<BlockedIp> BlockedIps { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Apply all configurations from the current assembly (The files we just created)
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 2. Apply Soft Delete Global Query Filter dynamically
        // این روش حرفه‌ای باعث می‌شود لازم نباشد برای هر Entity دستی فیلتر بنویسید.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // استفاده از متد کمکی برای اعمال فیلتر IsDeleted
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }
    }

    // متد کمکی برای رفلکشن
    static readonly MethodInfo SetGlobalQueryMethod = typeof(AppDbContext)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQuery));

    public void SetGlobalQuery<T>(ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Content { get; set; } 
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? Error { get; set; }
}