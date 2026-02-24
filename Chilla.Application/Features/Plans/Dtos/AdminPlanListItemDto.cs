namespace Chilla.Application.Features.Plans.Dtos;

public record AdminPlanListItemDto(
    Guid Id,
    string Title,
    decimal Price,
    int DurationInDays,
    bool IsActive,
    int TotalTasksCount // تعداد کل تسک‌های تعریف شده برای این چله
);