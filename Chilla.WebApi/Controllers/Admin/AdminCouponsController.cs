using Chilla.Application.Features.Coupons.Commands;
using Chilla.Application.Features.Coupons.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers.Admin;

[Route("api/admin/coupons")]
[ApiController]
[Authorize(Roles = "SuperAdmin")] 
public class AdminCouponsController : Controller
{
    private readonly IMediator _mediator;

    public AdminCouponsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // CREATE 
    [HttpPost]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponCommand command)
    {
        var couponId = await _mediator.Send(command);
        return Ok(new { Message = "کد تخفیف با موفقیت ایجاد شد.", CouponId = couponId });
    }

    // READ ALL 
    [HttpGet]
    public async Task<IActionResult> GetAllCoupons()
    {
        var coupons = await _mediator.Send(new GetAdminCouponsQuery());
        return Ok(coupons);
    }

    // READ SINGLE 
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCouponById(Guid id)
    {
        var coupon = await _mediator.Send(new GetAdminCouponByIdQuery(id));
        return Ok(coupon);
    }

    // UPDATE 
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] UpdateCouponCommand command)
    {
        if (id != command.Id) return BadRequest("آیدی ارسال شده با آیدی بدنه درخواست مغایرت دارد.");
        
        await _mediator.Send(command);
        return Ok(new { Message = "کد تخفیف با موفقیت ویرایش شد." });
    }

    // DELETE 
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCoupon(Guid id)
    {
        await _mediator.Send(new DeleteCouponCommand(id));
        return Ok(new { Message = "کد تخفیف با موفقیت حذف شد." });
    }
}