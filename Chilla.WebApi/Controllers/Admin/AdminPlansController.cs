using Chilla.Application.Features.Plans.Commands.CreatePlan;
using Chilla.Application.Features.Plans.Commands.DeletePlan;
using Chilla.Application.Features.Plans.Commands.UpdatePlan;
using Chilla.Application.Features.Plans.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers.Admin;

/// <summary>
/// مدیریت پلن‌ها و چله‌ها توسط مدیریت کل سیستم
/// </summary>
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/plans")]
[ApiController]
public class AdminPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// ایجاد یک پلن (چله) جدید با جزئیات دقیق تسک‌ها
    /// </summary>
    /// <remarks>
    /// **راهنمای پیاده‌سازی فرانت‌اند برای ساختار Items:**
    /// 
    /// فیلد `Type` نشان‌دهنده نوع کامپوننت UI است:
    /// - `Counter`: برای ذکرها (نیاز به TargetCount دارد).
    /// - `Reading`: برای خواندن سوره یا دعا.
    /// - `PhysicalAction`: برای اعمالی مثل غسل یا نوشتن نامه.
    /// - `Donation`: برای صدقه و رد مظالم.
    /// - `Consumable`: برای خوردن/آشامیدن (مثل آب متبرک).
    /// 
    /// **نمونه Payload برای "چله پاکسازی ۴۰ روزه":**
    /// ```json
    /// {
    ///   "title": "چله پاکسازی عمیق",
    ///   "description": "چله ۴۰ روزه شامل تعهدنامه، دعای معراج، ذکر و غسل پایانی",
    ///   "price": 50000,
    ///   "durationInDays": 40,
    ///   "items": [
    ///     {
    ///       "startDay": 1,
    ///       "endDay": 40,
    ///       "taskName": "دعای معراج",
    ///       "type": "Reading",
    ///       "isMandatory": true,
    ///       "notificationType": "Push",
    ///       "scheduleConfig": {
    ///         "frequency": "Daily",
    ///         "requiresUnbrokenChain": true,
    ///         "instructions": ["دعا نباید با فاصله و قطع شدن خوانده شود."],
    ///         "warnings": ["در صورت فراموشی در یک روز، چله باطل و باید از ابتدا شروع شود."]
    ///       }
    ///     },
    ///     {
    ///       "startDay": 1,
    ///       "endDay": 40,
    ///       "taskName": "ذکر روزانه",
    ///       "type": "Counter",
    ///       "isMandatory": true,
    ///       "scheduleConfig": {
    ///         "targetCount": 33,
    ///         "frequency": "Daily",
    ///         "instructions": ["روزانه ۳۳ مرتبه ذکر استغفار گفته شود."]
    ///       }
    ///     },
    ///     {
    ///       "startDay": 1,
    ///       "endDay": 40,
    ///       "taskName": "رد مظالم / صدقه",
    ///       "type": "Donation",
    ///       "isMandatory": true,
    ///       "scheduleConfig": {
    ///         "frequency": "Weekly",
    ///         "frequencyValue": 2,
    ///         "instructions": ["هفته‌ای دو روز باید مبلغی به عنوان رد مظالم کنار گذاشته شود."]
    ///       }
    ///     },
    ///     {
    ///       "startDay": 40,
    ///       "endDay": 40,
    ///       "taskName": "غسل پایان چله",
    ///       "type": "PhysicalAction",
    ///       "isMandatory": true,
    ///       "scheduleConfig": {
    ///         "frequency": "Once",
    ///         "instructions": ["با آب، نمک و سرکه غسل انجام شود.", "سوره حمد در آب خوانده و فوت شود. مقداری نوشیده و مابقی در گوشه های خانه اسپری شود."],
    ///         "warnings": ["اخطار شدید: این آب به هیچ وجه نباید در فاضلاب ریخته شود!"]
    ///       }
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPlanById), new { id }, new { Id = id });
    }

    /// <summary>
    /// ویرایش یک پلن موجود
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanCommand command)
    {
        if (id != command.Id) return BadRequest("آیدی ارسال شده با آیدی بدنه درخواست مطابقت ندارد.");
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// حذف یک پلن
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        await _mediator.Send(new DeletePlanCommand(id));
        return NoContent();
    }

    /// <summary>
    /// دریافت جزئیات کامل یک پلن (همراه با تمام تسک‌ها و دستورالعمل‌ها)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        var query = new GetAdminPlanByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// دریافت لیست تمام پلن‌ها برای ادمین (با صفحه‌بندی)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAllPlans()
    {
        var query = new GetAdminPlansQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}