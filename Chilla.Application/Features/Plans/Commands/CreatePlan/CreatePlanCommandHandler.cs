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
        // 1. تبدیل DTO به مدل ورودی دامین برای بررسی افزونگی
        var domainInputs = request.Items.Select(x => new PlanTemplateItemInputModel(
            x.StartDay,
            x.EndDay,
            x.TaskName,
            x.Type,
            x.ConfigJson,
            x.IsMandatory,
            ParseNotifications(x.Notifications)
        )).ToList();

        // 2. اجرای بیزنس لاجیک هوشمند (جلوگیری از تکرار و داده اضافی)
        Plan.ValidateForRedundancy(domainInputs);

        // 3. ساخت Aggregate Root
        var plan = new Plan(request.Title, request.Description, request.Price, request.DurationInDays);

        // 4. افزودن آیتم‌ها (حالا که مطمئنیم بهینه هستند)
        foreach (var item in domainInputs)
        {
            plan.AddTaskTemplate(
                item.StartDay,
                item.EndDay,
                item.TaskName,
                item.Type,
                item.ConfigJson,
                item.IsMandatory,
                item.NotificationType
            );
        }

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