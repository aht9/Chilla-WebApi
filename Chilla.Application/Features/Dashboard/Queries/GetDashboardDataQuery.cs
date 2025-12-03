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
        // 1. دریافت اطلاعات کاربر
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) throw new KeyNotFoundException("User not found");

        var response = new DashboardDataDto
        {
            UserProfile = new UserProfileDto(user.Id, user.FirstName, user.LastName, user.Username, user.PhoneNumber)
        };

        // 2. سناریوی ۱: پروفایل ناقص
        if (!user.IsProfileCompleted())
        {
            response.State = DashboardState.ProfileIncomplete;
            return response;
        }

        // 3. بررسی اشتراک‌های فعال کاربر
        var subscriptions = await _subscriptionRepository.GetByUserIdWithProgressAsync(request.UserId, cancellationToken);
        var activeSubs = subscriptions.Where(s => s.Status == SubscriptionStatus.Active).ToList();

        // 4. سناریوی ۲: کاربر اشتراک فعال دارد
        if (activeSubs.Any())
        {
            response.State = DashboardState.HasActiveSubscription;
            response.ActiveSubscriptions = await MapToSubscriptionCardsAsync(activeSubs, cancellationToken);
        }
        // 5. سناریوی ۳: کاربر پروفایل دارد اما هیچ پلنی ندارد (یا همه تمام شده‌اند)
        else
        {
            response.State = DashboardState.NoSubscription;
            response.AvailablePlans = await MapToPlanDtosAsync(cancellationToken);
        }

        return response;
    }

    // --- Helper Methods to keep Handle method clean ---

    private async Task<List<SubscriptionCardDto>> MapToSubscriptionCardsAsync(List<UserSubscription> subscriptions, CancellationToken cancellationToken)
    {
        var result = new List<SubscriptionCardDto>();

        foreach (var sub in subscriptions)
        {
            // نکته پرفورمنس: در حالت ایده‌آل باید پلن‌ها را Cache کنیم یا با یک کوئری Join بزنیم
            // اما فعلاً برای شفافیت منطق، جداگانه واکشی می‌کنیم.
            var plan = await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken);
            if (plan == null) continue;

            // محاسبه دقیق پیشرفت
            var distinctDaysCompleted = sub.Progress.Select(p => p.ScheduledDate).Distinct().Count();
            var totalDays = plan.DurationInDays;
            var progressPercent = totalDays > 0 ? (int)((double)distinctDaysCompleted / totalDays * 100) : 0;

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
        var plans = await _planRepository.GetAllActivePlansAsync(cancellationToken);
        
        return plans.Select(p => new PlanDto(
            p.Id, 
            p.Title, 
            p.Price, 
            p.DurationInDays, 
            p.Description
        )).ToList();
    }
}