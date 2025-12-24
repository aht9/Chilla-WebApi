using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using Chilla.Domain.Specifications.SubscriptionSpecs;
using FluentValidation;
using MediatR;

namespace Chilla.Application.Features.Plans.Commands.DeletePlan;

public record DeletePlanCommand(Guid Id) : IRequest;

public class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand>
{
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePlanCommandHandler(
        IPlanRepository planRepository, 
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.Id, cancellationToken);
        if (plan == null) throw new NotFoundException(nameof(Plan), request.Id);

        // 1. قانون مهم بیزنس: بررسی استفاده از پلن
        // آیا کسی این پلن را خریده است؟
        var usageSpec = new SubscriptionsByPlanIdSpec(request.Id);
        var existingSubscriptionsCount = await _subscriptionRepository.CountAsync(usageSpec, cancellationToken);

        if (existingSubscriptionsCount > 0)
        {
            throw new ValidationException(
                "امکان حذف این پلن وجود ندارد زیرا توسط کاربران خریداری یا استفاده شده است. " +
                "می‌توانید آن را غیرفعال کنید.");
        }

        // 2. اگر استفاده نشده بود، حذف فیزیکی یا نرم (بسته به کانفیگ BaseEntity) انجام شود
        // چون BaseEntity احتمالا Soft Delete دارد، این متد فلگ IsDeleted را ست می‌کند
        _planRepository.Delete(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}