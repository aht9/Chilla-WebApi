using System.Text.Json;
using Chilla.Application.Features.Plans.Dtos;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Specifications.PlanSpecs;
using MediatR;

namespace Chilla.Application.Features.Plans.Queries;

public record GetAdminPlanByIdQuery(Guid Id) : IRequest<AdminPlanDetailsDto>;

public class GetAdminPlanByIdQueryHandler : IRequestHandler<GetAdminPlanByIdQuery, AdminPlanDetailsDto>
{
    private readonly IPlanRepository _planRepository;

    public GetAdminPlanByIdQueryHandler(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<AdminPlanDetailsDto> Handle(GetAdminPlanByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. استفاده از Spec برای دریافت پلن + آیتم‌ها
        var spec = new PlanByIdWithItemsSpec(request.Id);
        
        var plan = await _planRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (plan == null)
            throw new KeyNotFoundException($"Plan with ID {request.Id} not found.");

        // 2. مپ کردن به DTO
        return new AdminPlanDetailsDto(
            plan.Id,
            plan.Title,
            plan.Description,
            plan.Price,
            plan.DurationInDays,
            plan.IsActive,
            plan.IsDeleted,
            plan.CreatedAt,
            plan.Items.Select(MapItemToDto).ToList()
        );
    }

    private PlanItemAdminDto MapItemToDto(PlanTemplateItem item)
    {
        TaskScheduleDto? scheduleConfig = null;

        if (!string.IsNullOrWhiteSpace(item.ConfigJson))
        {
            try
            {
                scheduleConfig = JsonSerializer.Deserialize<TaskScheduleDto>(item.ConfigJson);
            }
            catch
            {
                // Log warning
            }
        }

        return new PlanItemAdminDto(
            item.Id,
            item.StartDay,
            item.EndDay,
            item.TaskName,
            item.Type,
            item.IsMandatory,
            item.RequiredNotifications, // <-- اصلاح شد: نام صحیح پراپرتی در Entity
            scheduleConfig
        );
    }
}