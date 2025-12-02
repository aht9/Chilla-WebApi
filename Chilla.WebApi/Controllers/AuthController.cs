using Chilla.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login-otp")]
    public async Task<IActionResult> Login([FromBody] LoginOtpRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var command = new LoginWithOtpCommand(request.PhoneNumber, request.Code, ipAddress);
        
        var result = await _mediator.Send(command);

        // Set Refresh Token in HttpOnly Cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Always true in production
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(30)
        };

        Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

        return Ok(new { token = result.AccessToken });
    }
}