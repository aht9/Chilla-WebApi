using System.Reflection;
using Chilla.Domain.Aggregates.InvoiceAggregate;
using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.RoleAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace Chilla.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    // --- 1. Aggregate Roots (اصلی‌ها) ---
    public DbSet<User> Users { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    
    // --- 2. Security & Logs ---
    public DbSet<BlockedIp> BlockedIps { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    // --- 3. Child Entities (اضافه شده پس از تغییر معماری به HasMany) ---
    // افزودن این‌ها باعث می‌شود EF Core راحت‌تر جداول آن‌ها را مدیریت کند
    // و مایگریشن‌ها دقیق‌تر تولید شوند.
    
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    
    public DbSet<PlanTemplateItem> PlanTemplateItems { get; set; }
    public DbSet<DailyProgress> DailyProgresses { get; set; }

    private IDbContextTransaction? _currentTransaction;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<OutboxMessage>().HasIndex(x => x.ProcessedDate);

        // اعمال تمام کانفیگ‌های موجود در اسمبلی (UserConfiguration, PlanConfiguration, ...)
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // اعمال فیلتر Soft Delete به صورت Global
        ApplySoftDeleteQueryFilter(builder);
    }

    private void ApplySoftDeleteQueryFilter(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // موجودیت‌های Owned نیاز به فیلتر جداگانه ندارند (چون همراه پدر لود می‌شوند)
            // اما چون ما اکثر آن‌ها را به Entity مستقل تبدیل کردیم، اکنون شامل این فیلتر می‌شوند.
            if (entityType.IsOwned()) continue;

            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }
    }

    static readonly MethodInfo SetGlobalQueryMethod = typeof(AppDbContext)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQuery));

    public void SetGlobalQuery<T>(ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    // --- Transaction Management ---

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
}

// کلاس OutboxMessage که برای الگوی Transactional Outbox استفاده می‌شود
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } 
    public string Content { get; set; } 
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? Error { get; set; }
}