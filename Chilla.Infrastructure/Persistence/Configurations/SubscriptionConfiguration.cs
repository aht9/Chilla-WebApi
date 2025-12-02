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
        // ایندکس ترکیبی برای جلوگیری از تکرار اشتراک فعال یک پلن برای یک کاربر (اختیاری بر اساس بیزنس)
        // builder.HasIndex(s => new { s.UserId, s.PlanId }).HasFilter("[Status] = 'Active'");

        // --- Daily Progress ---
        // جدول بسیار مهم و پر تراکنش
        builder.OwnsMany(s => s.Progress, progBuilder =>
        {
            progBuilder.ToTable("DailyProgresses");
            progBuilder.HasKey(dp => dp.Id);
            progBuilder.WithOwner().HasForeignKey("UserSubscriptionId");

            progBuilder.Property(dp => dp.Date).HasColumnType("date"); // فقط تاریخ، بدون ساعت
            
            // ایندکس برای گزارش‌گیری سریع
            progBuilder.HasIndex(dp => dp.Date);
            
            progBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Metadata.FindNavigation(nameof(UserSubscription.Progress))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
            
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}