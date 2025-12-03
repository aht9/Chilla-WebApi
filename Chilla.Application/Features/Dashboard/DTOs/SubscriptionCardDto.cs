namespace Chilla.Application.Features.Dashboard.DTOs;

public record SubscriptionCardDto(
    Guid SubscriptionId,
    string PlanTitle,
    int CompletedDays,
    int TotalDays,
    int ProgressPercentage,
    DateTime StartDate,
    DateTime? EndDate
);