namespace Chilla.Application.Features.Plans.Dtos;

public record NotificationPolicyDto(
    List<string> AllowedMethods, // ["Push", "Sms", "VoiceCall"]
    int MaxFreeSmsAllowed,
    int MaxFreeVoiceCallMinutes,
    decimal ExtraSmsPrice,
    decimal ExtraVoiceCallPricePerMinute
);