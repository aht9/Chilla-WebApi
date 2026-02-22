using Chilla.Application.Features.Plans.Dtos;
using MediatR;

namespace Chilla.Application.Features.Plans.Commands.CreatePlan;

public record CreatePlanCommand(
    string Title,
    string Description,
    decimal Price,
    int DurationInDays,
    List<PlanItemInputDto> Items
) : IRequest<Guid>;