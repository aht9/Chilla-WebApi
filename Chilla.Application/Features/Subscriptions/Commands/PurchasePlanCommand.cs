using System.Text.Json;
using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.PlanAggregate.ValueObjects;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using MediatR;

namespace Chilla.Application.Features.Subscriptions.Commands;

// اضافه شدن ترجیحات کاربر به کامند
public record PurchasePlanCommand(
    Guid PlanId,
    List<UserNotificationPreferenceDto> UserPreferences
) : IRequest<Guid>;

// DTO برای دریافت ترجیحات هر تسک از سمت کاربر
public record UserNotificationPreferenceDto(
    Guid PlanTemplateItemId,
    NotificationType ChosenMethods,
    List<int> NotifyOffsetsInMinutes,
    int RequestedExtraSms,
    int RequestedExtraVoiceCallMinutes
);

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

        // 1. یافتن پلن و آیتم‌هایش
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan == null || !plan.IsActive)
            throw new NotFoundException("Plan not found or inactive.");

        // 2. محاسبه قیمت نهایی چله بر اساس ترجیحات کاربر (با تمام جزئیات)
        decimal finalTotalPrice = CalculateTotalPlanPrice(plan, request.UserPreferences);

        // 3. ایجاد اشتراک
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(plan.DurationInDays);
        var requiresPayment = finalTotalPrice > 0;

        // ذخیره JSON ترجیحات کاربر تا در Background Job (مثل خواندن دعا) استفاده شود
        string preferencesJson = JsonSerializer.Serialize(request.UserPreferences);

        var subscription = new UserSubscription(
            userId,
            plan.Id,
            startDate,
            endDate,
            requiresPayment,
            preferencesJson
        );

        // 4. اگر نیازمند پرداخت است، فاکتور (Invoice) باید ثبت شود
        if (requiresPayment)
        {
            // در اینجا متد مربوط به Repository فاکتورها فراخوانی می‌شود
            // (بسته به پیاده‌سازی شما در InvoicesController/Invoice Aggregate)
            // _invoiceRepository.Add(new Invoice(userId, finalTotalPrice, ...))
        }

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // اگر سیستم پرداخت دارید، احتمالاً باید InvoiceId را برگردانید تا کاربر ریدایرکت شود
        return subscription.Id;
    }

    /// <summary>
    /// متد اصلی برای محاسبه قیمت پایه چله + هزینه‌های مازاد اطلاع‌رسانی
    /// </summary>
    private decimal CalculateTotalPlanPrice(Plan plan, List<UserNotificationPreferenceDto> userPreferences)
    {
        decimal totalPrice = plan.Price; // قیمت پایه چله که ادمین تعریف کرده

        if (userPreferences == null || !userPreferences.Any())
            return totalPrice; // کاربر هیچ تنظیمات خاصی برای نوتیفیکیشن نخواسته است

        foreach (var pref in userPreferences)
        {
            // پیدا کردن تسک مرتبط در پلن
            var planItem = plan.Items.FirstOrDefault(x => x.Id == pref.PlanTemplateItemId);
            if (planItem == null) continue;

            // تبدیل کانفیگ ادمین (JSON) به آبجکت
            var taskConfig = JsonSerializer.Deserialize<TaskScheduleConfig>(
                planItem.ConfigJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (taskConfig?.NotificationPolicy == null) continue;
            var policy = taskConfig.NotificationPolicy;

            // 1. اعتبارسنجی: آیا متدهای انتخابی کاربر توسط ادمین مجاز شده است؟
            if ((pref.ChosenMethods & policy.AllowedMethods) != pref.ChosenMethods)
            {
                throw new DomainException(
                    $"شما روش اطلاع‌رسانی‌ای (مثل تماس صوتی یا پیامک) را برای تسک '{planItem.TaskName}' انتخاب کرده‌اید که برای این تسک فعال/مجاز نیست.");
            }

            // 2. محاسبه تعداد دفعاتی که این تسک در طول کل چله اجرا می‌شود
            int totalTaskInstances = CalculateTotalTaskInstances(planItem, taskConfig);

            // 3. محاسبه هزینه پیامک‌های مازاد
            if (pref.ChosenMethods.HasFlag(NotificationType.Sms))
            {
                // تعداد کل پیامک‌ها = (تعداد دفعات اجرای تسک در چله) * (تعداد نوتیفیکیشن‌هایی که کاربر برای هر اجرا می‌خواهد)
                // مثلا: تسک روزانه برای 40 روز. کاربر گفته [-30, 0] (یعنی 30 دقیقه قبل و سر وقت). کل پیامک: 40 * 2 = 80 عدد
                int totalSmsNeeded = totalTaskInstances * (pref.NotifyOffsetsInMinutes?.Count ?? 1);

                // کسر کردن سهمیه رایگان ادمین
                int extraSms = Math.Max(0, totalSmsNeeded - policy.MaxFreeSmsAllowed);

                // اضافه کردن پیامک‌های دستی که کاربر اضافه خواسته (در صورت وجود)
                extraSms += pref.RequestedExtraSms;

                totalPrice += extraSms * policy.ExtraSmsPrice;
            }

            // 4. محاسبه هزینه تماس صوتی مازاد (بر حسب دقیقه)
            if (pref.ChosenMethods.HasFlag(NotificationType.VoiceCall))
            {
                // فرض می‌کنیم هر بار تماس برای خواندن دعای این تسک، زمان خاصی می‌برد
                // این زمان در DurationMinutes تنظیمات ذخیره شده است (یا میانگین ۵ دقیقه در نظر می‌گیریم)
                int callDurationPerTask = taskConfig.DurationMinutes ?? 5;

                // کل دقایق تماس در طول چله
                int totalCallMinutes =
                    totalTaskInstances * callDurationPerTask * (pref.NotifyOffsetsInMinutes?.Count ?? 1);

                int extraCallMins = Math.Max(0, totalCallMinutes - policy.MaxFreeVoiceCallMinutes);

                // اضافه کردن دقایق دستی کاربر
                extraCallMins += pref.RequestedExtraVoiceCallMinutes;

                totalPrice += extraCallMins * policy.ExtraVoiceCallPricePerMinute;
            }
        }

        return totalPrice;
    }

    /// <summary>
    /// متد کمکی برای محاسبه اینکه یک تسک چند بار در طول دوره (بسته به فرکانس) تکرار می‌شود.
    /// حل مشکل تسک‌های "هفتگی" یا "یکبار در کل چله".
    /// </summary>
    private int CalculateTotalTaskInstances(PlanTemplateItem planItem, TaskScheduleConfig config)
    {
        // بازه زمانی خود این تسک (ممکن است کل چله نباشد، مثلا فقط از روز ۱۰ تا ۲۰ باشد)
        int totalDays = planItem.EndDay - planItem.StartDay + 1;

        return config.Frequency switch
        {
            // روزانه: دقیقاً برابر با تعداد روزهای بازه
            FrequencyType.Daily => totalDays,

            // هفتگی: محاسبه تعداد هفته‌ها * مقداری که در هفته خواسته شده (مثلا ۲ بار)
            FrequencyType.Weekly => (int)Math.Ceiling(totalDays / 7.0) * (config.FrequencyValue ?? 1),

            // بازه‌ای: هر X روز یکبار
            FrequencyType.Interval => totalDays / Math.Max(1, config.FrequencyValue ?? 1),

            // یکبار در کل بازه (مثل غسل روز آخر)
            FrequencyType.Once => 1,

            // پیش‌فرض اگر چیزی تنظیم نشده بود
            _ => totalDays
        };
    }
}