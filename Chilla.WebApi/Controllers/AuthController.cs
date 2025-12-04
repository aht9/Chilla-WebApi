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
    
    // Chilla.WebApi/Controllers/AuthController.cs Update

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "توکن یافت نشد." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        try
        {
            var result = await _mediator.Send(new RefreshTokenCommand(refreshToken, ipAddress));
        
            // آپدیت کوکی با توکن جدید (چرخش)
            SetRefreshTokenCookie(result.RefreshToken);

            return Ok(new { 
                accessToken = result.AccessToken, 
                isProfileCompleted = result.IsProfileCompleted 
            });
        }
        catch (Exception)
        {
            // هر خطایی در تمدید یعنی کاربر باید دوباره لاگین کند
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { message = "نشست منقضی شده است." });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(refreshToken))
        {
            // ارسال کامند برای باطل کردن توکن در دیتابیس
            await _mediator.Send(new RevokeTokenCommand(refreshToken, ipAddress));
        }

        // پاک کردن کوکی
        Response.Cookies.Delete("refreshToken", new CookieOptions { Secure = true, SameSite = SameSiteMode.Strict });
    
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