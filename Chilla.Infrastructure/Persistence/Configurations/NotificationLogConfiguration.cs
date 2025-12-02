using Chilla.Domain.Aggregates.NotificationAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Target).HasMaxLength(255);
        builder.Property(n => n.ErrorMessage).HasMaxLength(1000);

        // ایندکس برای نمایش تاریخچه به کاربر
        builder.HasIndex(n => n.UserId);
        // ایندکس برای لاگ‌برداری ادمین
        builder.HasIndex(n => n.CreatedAt);
    }
}