using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using MediatR;

namespace Chilla.Application.Features.Plans.Commands.SignCovenant;

public record SignSubscriptionCovenantCommand(Guid SubscriptionId) : IRequest;

public class SignSubscriptionCovenantCommandHandler : IRequestHandler<SignSubscriptionCovenantCommand>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SignSubscriptionCovenantCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SignSubscriptionCovenantCommand request, CancellationToken cancellationToken)
    {
        // دریافت آیدی کاربر لاگین شده برای مسائل امنیتی
        var userId = _currentUserService.UserId;

        // واکشی اشتراک از دیتابیس
        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        // اعتبارسنجی: آیا این اشتراک وجود دارد و متعلق به همین کاربر است؟
        if (subscription == null || subscription.UserId != userId)
            throw new NotFoundException("اشتراک مورد نظر یافت نشد یا متعلق به شما نیست.");

        // فراخوانی متد دامین که در مراحل قبل نوشتیم
        // این متد بررسی می‌کند که چله فعال باشد و کاربر قبلا تعهدنامه را امضا نکرده باشد
        subscription.SignRetroactiveCovenant();

        // ذخیره تغییرات در دیتابیس
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}