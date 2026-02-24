using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using MediatR;

namespace Chilla.Application.Features.Plans.Commands;

public record SubmitDailyProgressCommand(
    Guid SubscriptionId,
    Guid TaskId,
    int DayNumber,
    int CountCompleted,
    bool IsDone
) : IRequest;

public class SubmitDailyProgressCommandHandler : IRequestHandler<SubmitDailyProgressCommand>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SubmitDailyProgressCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SubmitDailyProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // ۱. دریافت اشتراک کاربر (همراه با لیست پیشرفت‌های قبلی)
        // فرض بر این است که متد GetByIdWithProgressesAsync در ریپازیتوری شما وجود دارد
        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription == null || subscription.UserId != userId)
            throw new NotFoundException("اشتراک مورد نظر یافت نشد یا متعلق به شما نیست.");

        // ۲. محاسبه روز فعلی چله
        var today = DateTime.UtcNow.Date;
        var startDayDate = subscription.StartDate.Date;
        
        int currentDay = today >= startDayDate 
            ? (int)(today - startDayDate).TotalDays + 1 
            : 0;

        // ۳. اعتبارسنجی‌های زمانی (Time Validations)
        if (currentDay == 0)
            throw new DomainException("زمان شروع این چله هنوز فرا نرسیده است.");

        if (request.DayNumber > currentDay)
            throw new DomainException("شما نمی‌توانید تسک‌های روزهای آینده را پیشاپیش ثبت کنید.");

        // ۴. منطق حساس تعهدنامه (Covenant Logic) برای روزهای گذشته
        if (request.DayNumber < currentDay && !subscription.HasSignedCovenant)
        {
            // کلمه عبور CovenantRequired در ابتدای خطا قرار داده شده تا فرانت‌اند بتواند آن را پارس کرده و پاپ‌آپ را نشان دهد
            throw new DomainException("CovenantRequired: زمان ثبت این تسک گذشته است. برای ویرایش روزهای گذشته ابتدا باید فرم تعهدنامه را امضا کنید.");
        }

        bool isLateEntry = request.DayNumber < currentDay;
        
        // ۵. ثبت یا به‌روزرسانی پیشرفت
        // متد RecordTaskProgress باید در کلاس UserSubscription (Aggregate Root) نوشته شده باشد 
        // تا یک رکورد جدید به لیست DailyProgresses اضافه کند یا در صورت وجود، آن را آپدیت کند.
        bool requiresUnbrokenChain = false; // این را از TaskScheduleDto تسک مربوطه بخوانید (t.ConfigJson)
        

        subscription.RecordTaskProgress(
            request.TaskId, 
            request.DayNumber, 
            request.IsDone, 
            request.CountCompleted, 
            isLateEntry,
            requiresUnbrokenChain // <--- پارامتر جدید
        );
        // ۶. ذخیره تغییرات در دیتابیس
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
