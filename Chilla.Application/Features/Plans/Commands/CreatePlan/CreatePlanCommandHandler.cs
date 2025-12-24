using System.Text.Json;
using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Plans.Commands.CreatePlan;

public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, Guid>
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePlanCommandHandler(IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new Plan(request.Title, request.Description, request.Price, request.DurationInDays);

        var domainItems = request.Items.Select(i => new PlanTemplateItemInputModel(
            i.StartDay,
            i.EndDay,
            i.TaskName,
            i.Type,
            JsonSerializer.Serialize(i.ScheduleConfig), // تبدیل کانفیگ به JSON برای ذخیره
            i.IsMandatory,
            i.NotificationType
        )).ToList();

        // 2. اجرای بیزنس لاجیک هوشمند (جلوگیری از تکرار و داده اضافی)
        plan.ReplaceTemplateItems(domainItems);
        // 5. ذخیره
        await _planRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }

    // متد کمکی برای تبدیل لیست رشته‌ها به Flag Enum
    private NotificationType ParseNotifications(List<string> notifications)
    {
        NotificationType result = NotificationType.None;
        foreach (var noteStr in notifications)
        {
            if (Enum.TryParse<NotificationType>(noteStr, true, out var parsedEnum))
            {
                result |= parsedEnum; // Bitwise OR
            }
        }
        return result;
    }
}