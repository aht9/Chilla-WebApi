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
        // 1. استفاده از Spec اصلاح شده در مرحله اول (true برای لود کردن آیتم‌ها)
        var spec = new ActivePlansSpec(includeDetails: true);
        var plans = await _planRepository.ListAsync(spec, cancellationToken);

        // 2. اصلاح ترتیب پارامترها و مپ کردن صحیح آیتم‌ها
        return plans.Select(p => new PlanDto(
            p.Id,                                       // 1. Id
            p.Title,                                    // 2. Title
            p.Price,                                    // 3. Price (decimal) - جایش اصلاح شد
            p.DurationInDays,                           // 4. Duration
            p.Description,                              // 5. Description (string?) - جایش اصلاح شد
            p.Items.Select(i => new PlanItemDto(        // 6. Items (List<PlanItemDto>)
                i.TaskName, 
                i.IsMandatory
            )).ToList()
        )).ToList();
    }
}