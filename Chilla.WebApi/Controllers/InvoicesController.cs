using Chilla.Application.Features.Invoices.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers;

[Route("api/invoices")]
[ApiController]
[Authorize] 
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// دریافت لیست تمام فاکتورها و سوابق سفارشات کاربر فعلی
    /// </summary>
    [HttpGet("my-invoices")]
    public async Task<IActionResult> GetMyInvoices()
    {
        var invoices = await _mediator.Send(new GetUserInvoicesQuery());
        
        // برگرداندن ساختار استاندارد
        return Ok(new 
        { 
            Count = invoices.Count, 
            Data = invoices 
        });
    }
}