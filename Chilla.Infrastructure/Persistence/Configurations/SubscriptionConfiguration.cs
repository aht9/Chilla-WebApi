using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chilla.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");
        
        builder.HasKey(s => s.Id);

        // تبدیل وضعیت (Enum) به رشته در دیتابیس برای خوانایی بهتر
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // ایندکس‌ها برای جستجوهای پرتکرار
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.PlanId);

        // --- پیکربندی فیلدهای جدید ---
        
        // فیلد تعهدنامه با مقدار پیش‌فرض false
        builder.Property(s => s.HasSignedCovenant)
            .HasDefaultValue(false)
            .IsRequired();

        // فیلدهای اختیاری
        builder.Property(s => s.InvoiceId).IsRequired(false);
        builder.Property(s => s.NotificationPreferencesJson).IsRequired(false);
        builder.Property(s => s.EndDate).IsRequired(false);

        // --- تنظیم ارتباط یک به چند با DailyProgress ---
        
        // به EF Core می‌گوییم که برای مقداردهی کالکشن از فیلد خصوصی _dailyProgresses استفاده کند
        builder.Metadata.FindNavigation(nameof(UserSubscription.DailyProgresses))?
               .SetPropertyAccessMode(PropertyAccessMode.Field);

        // ارتباط HasMany به جای پراپرتی منسوخ شده Progress از پراپرتی جدید DailyProgresses استفاده می‌کند
        // همچنین از کلید خارجی صریح SubscriptionId استفاده کردیم (Shadow FK حذف شد)
        builder.HasMany(s => s.DailyProgresses)
            .WithOne()
            .HasForeignKey(dp => dp.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);   // در صورت حذف چله، پیشرفت‌های روزانه نیز حذف می‌شوند
            
        // مدیریت همزمانی برای جلوگیری از تداخل آپدیت‌ها
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}