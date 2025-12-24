using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Subscriptions.Commands;
public record PurchasePlanCommand(Guid PlanId) : IRequest<Guid>;

public class PurchasePlanCommandHandler : IRequestHandler<PurchasePlanCommand, Guid>
{
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public PurchasePlanCommandHandler(
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(PurchasePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // 1. یافتن پلن
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan == null || !plan.IsActive) 
            throw new NotFoundException("Plan not found or inactive.");

        // 2. بررسی اینکه کاربر قبلاً اشتراک فعال برای این پلن نداشته باشد
        // (این منطق می‌تواند بسته به بیزنس متفاوت باشد، مثلا شاید بتواند تمدید کند)
        
        // 3. ایجاد اشتراک
        // محاسبه تاریخ پایان بر اساس Duration پلن
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(plan.DurationInDays);

        var subscription = new UserSubscription(
            userId, 
            plan.Id, 
            startDate, 
            endDate, 
            plan.Price > 0 // IsPaid
        );

        // اگر قیمت دارد، وضعیت پرداخت باید Pending باشد و کاربر به درگاه برود
        // اینجا فرض ساده: اگر رایگان است فعال می‌شود، اگر پولی است منطق پرداخت جداست
        if (plan.Price > 0)
        {
            // منطق ارجاع به درگاه پرداخت (Invoice creation) اینجا قرار می‌گیرد
            // فعلا اشتراک ایجاد می‌شود اما شاید IsActive فالس باشد تا پرداخت تایید شود
        }

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }
}