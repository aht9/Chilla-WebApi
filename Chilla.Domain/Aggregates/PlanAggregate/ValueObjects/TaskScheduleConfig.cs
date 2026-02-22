namespace Chilla.Domain.Aggregates.PlanAggregate.ValueObjects;

public record TaskScheduleConfig(
    // تنظیمات پایه
    int? TargetCount, // برای نوع Counter (مثلا 33 بار)
    TimeReferenceType? TimeRef, // زمانبندی (قبل از اذان و...)
    int? StartOffsetMinutes,
    int? DurationMinutes,

    // تنظیمات فرکانس (حل مشکل صدقه ۲ بار در هفته)
    FrequencyType Frequency, // نوع فرکانس
    int? FrequencyValue, // مقدار فرکانس (مثلا 2 برای دو بار در هفته)

    // تنظیمات محتوایی و نمایشی (حل مشکل دستورالعمل و هشدار)
    string? Description, // توضیح کلی
    List<string>? Instructions, // مراحل اجرا (مثلا: ۱. خواندن سوره ۲. فوت کردن ۳. پاشیدن)
    List<string>? Warnings, // هشدارها (مثلا: آب در فاضلاب ریخته نشود!)

    // اضافه شدن سیاست اطلاع رسانی
    NotificationPolicyConfig NotificationPolicy,

    // تنظیمات قواعد اجرای چله (حل مشکل دعای معراج بدون فاصله)
    bool RequiresUnbrokenChain = false, // آیا در صورت وقفه چله باطل/ریست می‌شود؟
    string? PostPlanNotes = null // کارهایی که بعد از چله باید انجام شود (مثل خواندن نامه هر ۲ ماه)
);