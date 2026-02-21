using Chilla.Application.Features.Coupons.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers;

[Route("api/coupons")]
[ApiController]
[Authorize] 
public class CouponsController : Controller
{
    private readonly IMediator _mediator;

    public CouponsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponQuery query)
    {
        var result = await _mediator.Send(query);
        if (!result.IsValid)
        {
            return BadRequest(new { result.Message, result.PayableAmount });
        }

        return Ok(result);
    }
}