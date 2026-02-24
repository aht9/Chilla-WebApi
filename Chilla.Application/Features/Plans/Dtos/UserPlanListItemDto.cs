namespace Chilla.Application.Features.Plans.Dtos;

public record UserPlanListItemDto(
    Guid SubscriptionId,
    Guid PlanId,
    string Title,
    DateTime StartDate,
    DateTime? EndDate,
    string Status,          // وضعیت: مثلاً Active, Completed, Failed
    int DurationInDays,     // کل روزهای چله (مثلاً 40 روز)
    int DaysPassed,         // چند روز از چله گذشته است
    int ProgressPercentage  // درصد پیشرفت کلی (از 0 تا 100)
);