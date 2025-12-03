namespace Chilla.Application.Features.Dashboard.DTOs;

public enum DashboardState
{
    /// <summary>
    /// اطلاعات پروفایل ناقص است (نام/نام‌خانوادگی)
    /// </summary>
    ProfileIncomplete = 1,

    /// <summary>
    /// پروفایل کامل است اما اشتراک فعالی ندارد (نمایش لیست خرید)
    /// </summary>
    NoSubscription = 2,

    /// <summary>
    /// کاربر دارای اشتراک فعال است (نمایش کارت‌های چله)
    /// </summary>
    HasActiveSubscription = 3
}