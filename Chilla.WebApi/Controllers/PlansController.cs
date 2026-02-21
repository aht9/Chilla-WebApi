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

    // مشاهده لیست پلن‌های قابل خرید
    [AllowAnonymous] 
    [HttpGet]
    public async Task<IActionResult> GetActivePlans()
    {
        var query = new GetActivePlansQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // خرید یا افزودن پلن به داشبورد
    [HttpPost("{id}/purchase")]
    public async Task<IActionResult> PurchasePlan(Guid id)
    {
        var command = new PurchasePlanCommand(id);
        var subscriptionId = await _mediator.Send(command);
        return Ok(new { SubscriptionId = subscriptionId, Message = "Plan added successfully." });
    }
}