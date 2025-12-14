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

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. کانفیگ‌های خاص که در فایل جدا ندارند
        builder.Entity<OutboxMessage>().HasIndex(x => x.ProcessedDate);

        // 2. اعمال تمام کانفیگ‌های جداگانه (UserConfig, PlanConfig, etc.)
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 3. اعمال تنظیمات سراسری (Global Configurations)
        // شامل: Soft Delete و تولید خودکار ID
        ApplyGlobalConfigurations(builder);
    }

    private void ApplyGlobalConfigurations(ModelBuilder builder)
    {
        // کش کردن متد جهت جلوگیری از Reflection تکراری در هر دور حلقه
        var setGlobalQueryMethod = typeof(AppDbContext)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQuery));

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // از موجودیت‌های Owned (که جدول مستقل ندارند) عبور می‌کنیم
            if (entityType.IsOwned()) continue;

            // بررسی اینکه آیا این کلاس از BaseEntity ارث برده است یا خیر
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // A. تنظیم استراتژی تولید شناسه 
                builder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Id))
                    .ValueGeneratedOnAdd();

                // B. اعمال فیلتر Soft Delete به صورت داینامیک
                var method = setGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }
    }

    // این متد توسط Reflection صدا زده می‌شود
    public void SetGlobalQuery<T>(ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
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