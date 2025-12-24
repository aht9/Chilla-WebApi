using Chilla.Application.Features.Dashboard.DTOs;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Specifications.PlanSpecs;
using MediatR;

namespace Chilla.Application.Features.Plans.Queries.GetActivePlans;

public record GetActivePlansQuery : IRequest<List<PlanDto>>;

public class GetActivePlansQueryHandler : IRequestHandler<GetActivePlansQuery, List<PlanDto>>
{
    private readonly IPlanRepository _planRepository;

    public GetActivePlansQueryHandler(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<List<PlanDto>> Handle(GetActivePlansQuery request, CancellationToken cancellationToken)
    {
        // فرض بر این است که متد GetListAsync یا مشابه در ریپوزیتوری وجود دارد
        // اگر از Specification استفاده می‌کنید:
        var plans = await _planRepository.ListAsync(new ActivePlansSpec(), cancellationToken);

        return plans.Select(p => new PlanDto(
            p.Id,
            p.Title,
            p.Description,
            p.Price,
            p.DurationInDays,
            p.Items.Count
        )).ToList();
    }
}