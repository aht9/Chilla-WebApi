using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Application.Features.Plans.Dtos;

public record AdminPlanDetailsDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    int DurationInDays,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt,
    List<PlanItemAdminDto> Items
);

public record PlanItemAdminDto(
    Guid Id,
    int StartDay,
    int EndDay,
    string TaskName,
    TaskType Type,
    bool IsMandatory,
    NotificationType NotificationType,
    TaskScheduleDto? ScheduleConfig // این آبجکت از روی ConfigJson ساخته می‌شود
);

// این همان کلاسی است که قبلا تعریف کردیم، اینجا برای DTO خروجی استفاده می‌شود
public record TaskScheduleDto(
    int TargetCount, 
    string TimeReference, 
    int StartOffsetMinutes, 
    int DurationMinutes,
    string? Description
);