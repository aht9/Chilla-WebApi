using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Application.Features.Plans.Dtos;

public record PlanDashboardDto(
    Guid SubscriptionId,
    int CurrentDay,           // روز چندم چله هستیم (مثلا روز 5 از 40)
    int DurationInDays,       // کل روزهای چله
    bool HasSignedCovenant,   // آیا تعهدنامه را امضا کرده است؟ (برای باز کردن قفل روزهای قبل)
    List<TodayTaskDto> TodayTasks,       // لیست تسک‌های مخصوص امروز
    List<PastDayStatusDto> PastDaysStatus // وضعیت روزهای گذشته
);

public record TodayTaskDto(
    Guid TaskId,
    string TaskName,
    TaskType Type,
    bool IsMandatory,
    TaskScheduleDto? ScheduleConfig, // تنظیمات و هشدارهای تسک
    bool IsCompleted,                // آیا کاربر امروز این را انجام داده؟
    int CountCompleted               // اگر از نوع Counter است، چند تا از ذکرها را گفته؟
);

public record PastDayStatusDto(
    int DayNumber,
    bool IsFullyCompleted // آیا تمام تسک‌های اجباری این روز تیک خورده‌اند؟
);