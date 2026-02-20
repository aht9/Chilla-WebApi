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
        if (user == null) throw new KeyNotFoundException("User not found");

        var response = new DashboardDataDto
        {
            UserProfile = new UserProfileDto(user.Id, user.FirstName, user.LastName, user.Username, user.Email, user.PhoneNumber)
        };

        if (!user.IsProfileCompleted())
        {
            response.State = DashboardState.ProfileIncomplete;
            return response;
        }

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
        foreach (var sub in subscriptions)
        {
            var plan = await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken);
            if (plan == null) continue;

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
            p.Description,
            // مپ کردن پارامتر ششم (Items)
            p.Items.Select(i => new PlanItemDto(i.TaskName, i.IsMandatory)).ToList()
        )).ToList();
    }
}