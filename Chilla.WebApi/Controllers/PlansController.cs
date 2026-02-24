using Chilla.Application.Features.Plans.Commands;
using Chilla.Application.Features.Plans.Commands.SignCovenant;
using Chilla.Application.Features.Plans.Dtos;
using Chilla.Application.Features.Plans.Queries;
using Chilla.Application.Features.Plans.Queries.GetActivePlans;
using Chilla.Application.Features.Subscriptions.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers;

[Authorize]
[Route("api/plans")]
[ApiController]
public class PlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// مشاهده لیست پلن‌های موجود در فروشگاه اپلیکیشن برای خرید
    /// </summary>
    [AllowAnonymous]
    [HttpGet("store")]
    public async Task<IActionResult> GetActivePlans()
    {
        var query = new GetActivePlansQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // ==========================================
    // بخش جدید: داشبورد کاربری و مدیریت پیشرفت
    // ==========================================

    /// <summary>
    /// دریافت لیست پلن‌های خریداری شده کاربر (فعال و پایان‌یافته) به همراه درصد پیشرفت
    /// </summary>
    /// <remarks>
    /// این API لیست چله‌های مختص کاربری که لاگین کرده را برمی‌گرداند.
    /// درصد پیشرفت بر اساس (تعداد روزهای سپری شده / طول کل چله) محاسبه می‌شود.
    /// </remarks>
    [HttpGet("my-plans")]
    [ProducesResponseType(typeof(List<UserPlanListItemDto>), 200)]
    public async Task<IActionResult> GetMyPlans()
    {
        var query = new GetUserSubscriptionsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// داشبورد اختصاصی یک پلن (نمایش تسک‌های امروز، روزهای گذشته، و وضعیت تعهدنامه)
    /// </summary>
    /// <remarks>
    /// این متد اطلاعات جامعی برای رندر UI برمی‌گرداند:
    /// - CurrentDay: روز چندم چله هستیم؟
    /// - TodayTasks: لیست تسک‌های امروز با وضعیت انجام (آیا کانتر دارد؟ دستورالعمل خاص دارد؟)
    /// - HasSignedCovenant: آیا کاربر برای این چله تعهدنامه پر کرده است؟ (برای باز کردن قفل روزهای قبل)
    /// - PastDaysStatus: وضعیت روزهای گذشته (کدام روزها ناقص مانده‌اند).
    /// </remarks>
    [HttpGet("my-plans/{subscriptionId}/dashboard")]
    [ProducesResponseType(typeof(PlanDashboardDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlanDashboard(Guid subscriptionId)
    {
        var query = new GetUserPlanDashboardQuery(subscriptionId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    public record TaskProgressRequest(int CountCompleted, bool IsDone);

    /// <summary>
    /// ثبت پیشرفت برای یک تسک در یک روز خاص
    /// </summary>
    /// <remarks>
    /// **نکته بسیار مهم منطقی برای فرانت‌اند:**
    /// 1. اگر کاربر در روز فعلی (یا پس از امضای تعهدنامه در روزهای قبل) تسکی را تیک بزند، عملیات با استاتوس 200 موفق می‌شود.
    /// 2. اگر کاربر بخواهد تسک روزهای قبل را تیک بزند اما تعهدنامه (Covenant) نداشته باشد، این API یک خطای 400 برمی‌گرداند 
    /// که متن پیام (Message) آن با کلمه `CovenantRequired` شروع می‌شود.
    /// 
    /// **نحوه هندل کردن در فرانت‌اند:**
    /// ```javascript
    /// if (error.response.status === 400 && error.response.data.message.includes("CovenantRequired")) {
    ///     // 1. نمایش Modal تعهدنامه مذهبی به کاربر
    ///     // 2. در صورت تایید کاربر، فراخوانی API: POST /my-plans/{id}/sign-covenant
    ///     // 3. در صورت موفقیت، فراخوانی مجدد همین API (SubmitTaskProgress)
    /// }
    /// ```
    /// </remarks>
    [HttpPost("my-plans/{subscriptionId}/tasks/{taskId}/progress")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitTaskProgress(Guid subscriptionId, Guid taskId, [FromQuery] int day,
        [FromBody] TaskProgressRequest request)
    {
        var command = new SubmitDailyProgressCommand(
            SubscriptionId: subscriptionId,
            TaskId: taskId,
            DayNumber: day,
            CountCompleted: request.CountCompleted,
            IsDone: request.IsDone
        );

        await _mediator.Send(command);

        return Ok(new { Message = "پیشرفت با موفقیت ثبت شد." });
    }

    /// <summary>
    /// امضای تعهدنامه توسط کاربر برای باز کردن قفل ویرایش روزهای گذشته
    /// </summary>
    /// <remarks>
    /// کاربر با فراخوانی این API تعهد می‌دهد که تسک‌های روزهای گذشته را در دنیای واقعی انجام داده است 
    /// اما فراموش کرده در اپلیکیشن تیک بزند. با موفقیت این API، کاربر مجاز به ادیت روزهای گذشته می‌شود.
    /// </remarks>
    [HttpPost("my-plans/{subscriptionId}/sign-covenant")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SignCovenant(Guid subscriptionId)
    {
        // فراخوانی کامند مربوط به امضای تعهدنامه
        var command = new SignSubscriptionCovenantCommand(subscriptionId);
        await _mediator.Send(command);

        return Ok(new
        {
            Message = "تعهدنامه با موفقیت ثبت شد. اکنون می‌توانید تسک‌های روزهای گذشته را تیک بزنید."
        });
    }
}