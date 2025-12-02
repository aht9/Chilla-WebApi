using Chilla.Domain.Aggregates.SubscriptionAggregate;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");
        builder.HasKey(s => s.Id);

        // تبدیل وضعیت به رشته (Active, Completed, ...)
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Indexing for fast lookups
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.PlanId);

        // --- Daily Progress ---
        // جدول بسیار مهم و پر تراکنش
        builder.OwnsMany(s => s.Progress, progBuilder =>
        {
            progBuilder.ToTable("DailyProgresses");
            progBuilder.HasKey(dp => dp.Id);
            progBuilder.WithOwner().HasForeignKey("UserSubscriptionId");

            // اصلاح شد: Date -> ScheduledDate
            progBuilder.Property(dp => dp.ScheduledDate).HasColumnType("date"); // فقط تاریخ، بدون ساعت
            
            // اصلاح شد: ایندکس روی ScheduledDate
            progBuilder.HasIndex(dp => dp.ScheduledDate);
            
            // مپ کردن فیلدهای جدید
            progBuilder.Property(dp => dp.CompletedAt).IsRequired(false);
            progBuilder.Property(dp => dp.IsLateEntry).HasDefaultValue(false);
            progBuilder.Property(dp => dp.LateReason).HasMaxLength(500).IsRequired(false);

            progBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Metadata.FindNavigation(nameof(UserSubscription.Progress))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
            
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}