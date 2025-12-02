using Chilla.Domain.Aggregates.NotificationAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.Target).HasMaxLength(200);
        
        // ایندکس روی UserId برای جستجوی سریع تاریخچه پیام‌های کاربر
        builder.HasIndex(n => n.UserId);
    }
}