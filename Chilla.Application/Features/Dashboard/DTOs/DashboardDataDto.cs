using Chilla.Application.Features.Users.Dtos;

namespace Chilla.Application.Features.Dashboard.DTOs;

public class DashboardDataDto
{
    public DashboardState State { get; set; }
    
    // اطلاعات پایه کاربر (همیشه ارسال می‌شود)
    public UserProfileDto? UserProfile { get; set; }

    // فقط وقتی State == NoSubscription باشد پر می‌شود
    public List<PlanDto>? AvailablePlans { get; set; }

    // فقط وقتی State == HasActiveSubscription باشد پر می‌شود
    public List<SubscriptionCardDto>? ActiveSubscriptions { get; set; }
}