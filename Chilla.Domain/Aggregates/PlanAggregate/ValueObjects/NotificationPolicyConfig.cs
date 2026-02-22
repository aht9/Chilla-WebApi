using Chilla.Domain.Aggregates.NotificationAggregate;

namespace Chilla.Domain.Aggregates.PlanAggregate.ValueObjects;

public record NotificationPolicyConfig(
    NotificationType AllowedMethods,     // روش‌های مجاز (مثلاً Push و Sms مجاز است اما VoiceCall نه)
    int MaxFreeSmsAllowed,               // حداکثر تعداد پیامک رایگان در طول کل این چله
    int MaxFreeVoiceCallMinutes,         // حداکثر دقیقه تماس صوتی رایگان
    decimal ExtraSmsPrice,               // قیمت هر پیامک مازاد (مثلاً 100 تومان)
    decimal ExtraVoiceCallPricePerMinute // قیمت هر دقیقه تماس صوتی مازاد (مثلاً 1000 تومان)
);