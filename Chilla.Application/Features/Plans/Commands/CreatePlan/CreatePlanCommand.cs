using Chilla.Domain.Aggregates.PlanAggregate;
using MediatR;

namespace Chilla.Application.Features.Plans.Commands.CreatePlan;

public record CreatePlanCommand(
    string Title,
    string Description,
    decimal Price,
    int DurationInDays,
    List<PlanItemDto> Items
) : IRequest<Guid>;

public record PlanItemDto(
    int StartDay,
    int EndDay,
    string TaskName,
    TaskType Type,
    string ConfigJson,
    bool IsMandatory,
    List<string> Notifications // کاربر به صورت رشته می‌فرستد: ["Sms", "Site"]
);