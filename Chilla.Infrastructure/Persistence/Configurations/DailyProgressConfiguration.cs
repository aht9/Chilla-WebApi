using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class DailyProgressConfiguration : IEntityTypeConfiguration<DailyProgress>
{
    public void Configure(EntityTypeBuilder<DailyProgress> builder)
    {
        builder.ToTable("DailyProgresses");
        
        builder.HasKey(dp => dp.Id);

        // تنظیمات فیلدهای اصلی
        builder.Property(dp => dp.SubscriptionId).IsRequired();
        builder.Property(dp => dp.TaskId).IsRequired();
        builder.Property(dp => dp.DayNumber).IsRequired();

        // ایجاد ایندکس یکتا (Unique) برای جلوگیری از ثبت دیتای تکراری
        // یک کاربر در یک روز مشخص از یک چله، فقط یک رکورد پیشرفت برای یک تسک خاص می‌تواند داشته باشد
        builder.HasIndex(dp => new { dp.SubscriptionId, dp.TaskId, dp.DayNumber })
            .IsUnique()
            .HasDatabaseName("IX_DailyProgresses_Subscription_Task_Day");

        // مقادیر پیش‌فرض برای وضعیت انجام تسک‌ها
        builder.Property(dp => dp.IsDone).HasDefaultValue(false);
        builder.Property(dp => dp.CountCompleted).HasDefaultValue(0);

        // تنظیمات فیلدهای Nullable و تعهدنامه
        builder.Property(dp => dp.CompletedAt).IsRequired(false);
        builder.Property(dp => dp.IsLateEntry).HasDefaultValue(false);

        // مدیریت همزمانی (Concurrency)
        builder.Property(x => x.RowVersion).IsRowVersion();

        // تنظیم ارتباط با UserSubscription (اختیاری است اما برای شفافیت بهتر است نوشته شود)
        builder.HasOne<UserSubscription>()
            .WithMany(s => s.DailyProgresses)
            .HasForeignKey(dp => dp.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade); // در صورت حذف چله کاربر، پیشرفت‌هایش هم حذف شود
    }
}