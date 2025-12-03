using System.Security.Claims;
using Chilla.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers;


[ApiController]
[Route("api/dashboard")]
[Authorize] // تمام متدها نیاز به لاگین دارند
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// اندپوینت اصلی برای دریافت وضعیت صفحه اصلی اپلیکیشن
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboardState()
    {
        // دریافت ID کاربر جاری از توکن
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var query = new GetDashboardDataQuery(userId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
}