namespace Chilla.Domain.Aggregates.PlanAggregate.ValueObjects;

public record TaskScheduleConfig(
    int TargetCount,             // تعداد تکرار (مثلا ۱۰ بار ذکر)
    TimeReferenceType TimeRef,   // مبنای زمانی
    int StartOffsetMinutes,      // فاصله از مبنا (مثلا ۶۰- دقیقه یعنی ۱ ساعت قبل)
    int DurationMinutes,         // مهلت انجام (مثلا ۶۰ دقیقه)
    string? Description          // توضیحات تکمیلی (نحوه انجام)
);