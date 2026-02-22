using Chilla.Domain.Aggregates.NotificationAggregate;

namespace Chilla.Domain.Aggregates.SubscriptionAggregate.ValueObjects;

public record UserNotificationPreference(
    NotificationType ChosenMethods,      // کاربر کدام روش‌ها را انتخاب کرده؟ (باید با AllowedMethods ادمین چک شود)
    
    // لیستی از فاصله‌های زمانی برای یادآوری (به دقیقه)
    // مثلاً: [60, 15, 0] یعنی 1 ساعت قبل، 15 دقیقه قبل و دقیقاً سر وقت
    List<int> NotifyOffsetsInMinutes, 
    
    int RequestedExtraSms,               // کاربر چند پیامک اضافه درخواست داده است؟
    int RequestedExtraVoiceCallMinutes   // کاربر چند دقیقه تماس صوتی اضافه برای خواندن دعا خواسته است؟
);