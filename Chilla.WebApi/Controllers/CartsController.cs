using Chilla.Application.Features.Carts.Commands;
using Chilla.Application.Features.Carts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers;

[Route("api/cart")]
[ApiController]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCart()
    {
        var cart = await _mediator.Send(new GetUserCartQuery());
        return Ok(cart);
    }

    [HttpPost("items/{planId:guid}")]
    public async Task<IActionResult> AddItemToCart(Guid planId)
    {
        await _mediator.Send(new AddToCartCommand(planId));
        return Ok(new { Message = "چله با موفقیت به سبد خرید اضافه شد." });
    }

    [HttpDelete("items/{planId:guid}")]
    public async Task<IActionResult> RemoveItemFromCart(Guid planId)
    {
        await _mediator.Send(new RemoveFromCartCommand(planId));
        return Ok(new { Message = "چله از سبد خرید حذف شد." });
    }

    [HttpPost("coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode)) return BadRequest("کد تخفیف را وارد کنید.");

        await _mediator.Send(new ApplyCouponToCartCommand(couponCode));
        return Ok(new { Message = "کد تخفیف با موفقیت روی سبد شما اعمال شد." });
    }

    [HttpDelete("coupon")]
    public async Task<IActionResult> RemoveCoupon()
    {
        await _mediator.Send(new RemoveCouponFromCartCommand());
        return Ok(new { Message = "کد تخفیف حذف شد." });
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var result = await _mediator.Send(new CheckoutCartCommand());
        
        // اگر در فاز فعلی می‌خواهید جلوی خریدهای پولی را بگیرید، 
        // می‌توانید اینجا چک کنید و BadRequest برگردانید:
        if (result.RequiresPayment)
        {
            return BadRequest(new { 
                Message = "در حال حاضر اتصال به درگاه پرداخت امکان‌پذیر نیست. لطفاً از کد تخفیف ۱۰۰ درصدی استفاده نمایید.",
                PayableAmount = result.PayableAmount
            });
        }

        return Ok(result);
    }
}