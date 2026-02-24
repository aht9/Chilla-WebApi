using Chilla.Application.Features.Dashboard.DTOs;
using Chilla.Application.Features.Users.Dtos;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using MediatR;

namespace Chilla.Application.Features.Dashboard.Queries;

public record GetDashboardDataQuery(Guid UserId) : IRequest<DashboardDataDto>;

public class GetDashboardDataHandler : IRequestHandler<GetDashboardDataQuery, DashboardDataDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;

    public GetDashboardDataHandler(
        IUserRepository userRepository, 
        ISubscriptionRepository subscriptionRepository, 
        IPlanRepository planRepository)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<DashboardDataDto> Handle(GetDashboardDataQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) throw new KeyNotFoundException("کاربر یافت نشد.");

        var response = new DashboardDataDto
        {
            UserProfile = new UserProfileDto(user.Id, user.FirstName, user.LastName, user.Username, user.Email, user.PhoneNumber)
        };

        // بررسی تکمیل بودن پروفایل
        if (!user.IsProfileCompleted())
        {
            response.State = DashboardState.ProfileIncomplete;
            return response;
        }

        // واکشی اشتراک‌های کاربر به همراه پیشرفت‌ها (با Include که در ریپازیتوری وجود دارد)
        var subscriptions = await _subscriptionRepository.GetByUserIdWithProgressAsync(request.UserId, cancellationToken);
        var activeSubs = subscriptions.Where(s => s.Status == SubscriptionStatus.Active).ToList();

        if (activeSubs.Any())
        {
            response.State = DashboardState.HasActiveSubscription;
            response.ActiveSubscriptions = await MapToSubscriptionCardsAsync(activeSubs, cancellationToken);
        }
        else
        {
            response.State = DashboardState.NoSubscription;
            response.AvailablePlans = await MapToPlanDtosAsync(cancellationToken);
        }

        return response;
    }

    private async Task<List<SubscriptionCardDto>> MapToSubscriptionCardsAsync(List<UserSubscription> subscriptions, CancellationToken cancellationToken)
    {
        var result = new List<SubscriptionCardDto>();
        
        // --- بهینه‌سازی پرفورمنس (حل مشکل N+1) ---
        // به جای اینکه داخل حلقه برای هر اشتراک یک بار به دیتابیس کوئری بزنیم، 
        // آیدی تمام پلن‌ها را استخراج کرده و آن‌ها را یکجا از دیتابیس می‌خوانیم.
        var planIds = subscriptions.Select(s => s.PlanId).Distinct().ToList();
        
        // فرض بر این است که این متد در IPlanRepository شما وجود دارد (در غیر این صورت باید اضافه کنید)
        // var plans = await _context.Plans.Where(p => planIds.Contains(p.Id)).ToListAsync(cancellationToken);
        var plans = await _planRepository.GetPlansByIdsAsync(planIds, cancellationToken); 

        foreach (var sub in subscriptions)
        {
            var plan = plans.FirstOrDefault(p => p.Id == sub.PlanId);
            if (plan == null) continue;

            // --- انطباق با منطق جدید دامین ---
            // نام پراپرتی به DailyProgresses تغییر کرده است.
            // به جای ScheduledDate، از DayNumber استفاده می‌کنیم.
            // برای اطمینان، فقط روزهایی را می‌شماریم که حداقل یک تسک در آن تیک خورده باشد یا شمارنده‌اش بیشتر از 0 باشد.
            var distinctDaysCompleted = sub.DailyProgresses
                .Where(p => p.IsDone || p.CountCompleted > 0)
                .Select(p => p.DayNumber)
                .Distinct()
                .Count();

            var totalDays = plan.DurationInDays;
            
            // محاسبه درصد پیشرفت به صورت ایمن (جلوگیری از تقسیم بر صفر و گرد کردن درست)
            var progressPercent = totalDays > 0 
                ? (int)Math.Round((double)distinctDaysCompleted / totalDays * 100) 
                : 0;

            result.Add(new SubscriptionCardDto(
                sub.Id,
                plan.Title,
                distinctDaysCompleted,
                totalDays,
                progressPercent,
                sub.StartDate,
                sub.EndDate
            ));
        }
        return result;
    }

    private async Task<List<PlanDto>> MapToPlanDtosAsync(CancellationToken cancellationToken)
    {
        // فرض بر این است که متد GetAllActivePlansAsync وجود دارد و Items را هم Include می‌کند
        var plans = await _planRepository.GetAllActivePlansAsync(cancellationToken);
        
        return plans.Select(p => new PlanDto(
            p.Id, 
            p.Title, 
            p.Price, 
            p.DurationInDays, 
            p.Description,
            // مپ کردن تسک‌ها بر اساس پراپرتی‌های جدید PlanTemplateItem
            p.Items.Select(i => new PlanItemDto(i.TaskName, i.IsMandatory)).ToList()
        )).ToList();
    }
}