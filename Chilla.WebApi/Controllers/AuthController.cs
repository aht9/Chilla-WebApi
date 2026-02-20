using Chilla.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    // سناریوی ۱-a و ۲: ورود یا ثبت‌نام با شماره موبایل و OTP
    [HttpPost("login-otp")]
    public async Task<IActionResult> LoginWithOtp([FromBody] LoginOtpRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var command = new LoginWithOtpCommand(request.PhoneNumber, request.Code, ipAddress);
        
        var result = await _mediator.Send(command);

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new 
        { 
            accessToken = result.AccessToken,
            isProfileCompleted = result.IsProfileCompleted,
            message = result.Message
        });
    }

    // سناریوی ۱-b: ورود با نام کاربری و رمز عبور
    [HttpPost("login-password")]
    public async Task<IActionResult> LoginWithPassword([FromBody] LoginPasswordRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var command = new LoginWithPasswordCommand(request.Username, request.Password, ipAddress);

        var result = await _mediator.Send(command);

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new 
        { 
            accessToken = result.AccessToken, 
            isProfileCompleted = result.IsProfileCompleted,
            message = result.Message
        });
    }

    // درخواست کد OTP (مرحله اول لاگین با موبایل)
    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequest request)
    {
        var command = new RequestOtpCommand(request.PhoneNumber);
        await _mediator.Send(command);
        return Ok(new { message = "کد تایید ارسال شد." });
    }

    // تکمیل اطلاعات پروفایل (بعد از ثبت نام اولیه)
    [HttpPost("complete-profile")]
    // [Authorize] // بهتر است این متد نیاز به توکن داشته باشد که از مرحله قبل گرفته شده
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileRequest request)
    {
        // UserId را معمولاً از توکن استخراج می‌کنیم، اما اینجا طبق کامند شما پیش می‌رویم
        // در حالت واقعی: var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var command = new CompleteProfileCommand(
            request.UserId, 
            request.FirstName, 
            request.LastName, 
            request.Username, 
            request.Email, 
            request.Password);

        await _mediator.Send(command);
        return Ok(new { message = "پروفایل با موفقیت تکمیل شد." });
    }

    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        // دریافت توکن از کوکی (امنیت HttpOnly)
        var refreshToken = Request.Cookies["refreshToken"];
    
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "رفرش توکن یافت نشد." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    
        try 
        {
            var command = new RefreshTokenCommand(refreshToken, ipAddress);
            var result = await _mediator.Send(command);

            // جایگزینی کوکی قدیمی با جدید (Rotation)
            SetRefreshTokenCookie(result.RefreshToken);

            return Ok(new 
            { 
                accessToken = result.AccessToken,
                isProfileCompleted = result.IsProfileCompleted // شاید در این مدت پروفایلش را تکمیل کرده باشد
            });
        }
        catch (UnauthorizedAccessException)
        {
            // اگر تمدید شکست خورد، کوکی را پاک می‌کنیم تا کاربر لاگ‌اوت شود
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { message = "نشست کاربری منقضی شده است. لطفاً مجدداً وارد شوید." });
        }
    }

    [HttpPost("logout")]
    [Authorize] // فقط کاربر لاگین شده می‌تواند لاگ‌اوت کند
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            // اینجا می‌توانید یک Command برای Revoke کردن توکن در دیتابیس هم صدا بزنید
            // فعلاً کوکی را پاک می‌کنیم
        }

        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "خروج با موفقیت انجام شد." });
        
        
        
    }
    // --- Helper Methods ---

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // جاوااسکریپت دسترسی ندارد (امنیت XSS)
            Secure = true,   // فقط روی HTTPS (در دولوپمنت ممکن است فالس باشد)
            SameSite = SameSiteMode.Strict, // جلوگیری از CSRF
            Expires = DateTime.UtcNow.AddDays(30)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}

// DTOs for Controller Requests
public record LoginOtpRequest(string PhoneNumber, string Code);
public record RequestOtpRequest(string PhoneNumber);
public record LoginPasswordRequest(string Username, string Password);
public record CompleteProfileRequest(Guid UserId, string FirstName, string LastName, string Username, string? Email, string? Password);