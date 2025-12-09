using Chilla.Application.Features.Auth.Commands;
using Chilla.Application.Features.Auth.DTOs;
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

    // ----------------------------------------------------------------
    // 1. ورود / ثبت‌نام با OTP
    // ----------------------------------------------------------------
    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequest request)
    {
        var command = new RequestOtpCommand(request.PhoneNumber);
        await _mediator.Send(command);
        return Ok(new { message = "کد تایید با موفقیت ارسال شد." });
    }

    [HttpPost("login-otp")]
    public async Task<IActionResult> LoginWithOtp([FromBody] LoginOtpRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";


        var command = new LoginWithOtpCommand(request.PhoneNumber, request.Code, ipAddress);
        var result = await _mediator.Send(command);

        // ذخیره امن توکن‌ها در کوکی
        SetTokenCookies(result.AccessToken, result.RefreshToken);

        // نکته امنیتی: توکن‌ها را در Body برنمی‌گردانیم تا در LocalStorage ذخیره نشوند
        return Ok(new
        {
            isProfileCompleted = result.IsProfileCompleted,
            message = result.Message
        });
    }

    // ----------------------------------------------------------------
    // 2. ورود با رمز عبور (سناریوی جایگزین)
    // ----------------------------------------------------------------
    [HttpPost("login-password")]
    public async Task<IActionResult> LoginWithPassword([FromBody] LoginPasswordRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        var command = new LoginWithPasswordCommand(request.Username, request.Password, ipAddress);
        var result = await _mediator.Send(command);

        SetTokenCookies(result.AccessToken, result.RefreshToken);

        return Ok(new
        {
            isProfileCompleted = result.IsProfileCompleted,
            message = result.Message
        });
    }

    // ----------------------------------------------------------------
    // 3. تمدید توکن (Refresh Token Rotation)
    // ----------------------------------------------------------------
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        // دریافت رفرش توکن فقط از کوکی HttpOnly
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "توکن یافت نشد. لطفاً مجدداً وارد شوید." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        try
        {
            var command = new RefreshTokenCommand(refreshToken, ipAddress);
            var result = await _mediator.Send(command);

            // جایگزینی توکن‌های قدیمی با جدید (Rotation)
            SetTokenCookies(result.AccessToken, result.RefreshToken);

            return Ok(new
            {
                isProfileCompleted = result.IsProfileCompleted,
                message = "نشست کاربری تمدید شد."
            });
        }
        catch (Exception)
        {
            // اگر تمدید شکست خورد (توکن سوخته/نامعتبر)، کوکی‌ها را پاک می‌کنیم تا کاربر لاگ‌اوت شود
            ForceLogoutCookies();
            return Unauthorized(new { message = "نشست کاربری منقضی شده است." });
        }
    }

    // ----------------------------------------------------------------
    // 4. تکمیل پروفایل (کاربران جدید)
    // ----------------------------------------------------------------
    [HttpPost("complete-profile")]
    [Authorize] // اکنون که AccessToken در کوکی است، این اتریبیوت کار می‌کند
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileRequest request)
    {
        // در معماری صحیح، UserId را باید از Claims توکن استخراج کنیم نه ورودی کاربر
        // var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // اما فعلاً طبق درخواست شما از Body می‌خوانیم:

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

    // ----------------------------------------------------------------
    // 5. خروج امن (Logout)
    // ----------------------------------------------------------------
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            // ابطال توکن در دیتابیس
            await _mediator.Send(new RevokeTokenCommand(refreshToken, ipAddress));
        }

        // پاکسازی کوکی‌ها از مرورگر
        ForceLogoutCookies();

        return Ok(new { message = "خروج با موفقیت انجام شد." });
    }

    // ================================================================
    // Helper Methods (Private)
    // ================================================================

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // غیرقابل دسترسی برای JS (ضد XSS)
            Secure = true, // فقط HTTPS (در محیط Dev لوکال هم معمولاً کار می‌کند اگر Https باشد)
            SameSite = SameSiteMode.Strict, // جلوگیری از CSRF (بسیار مهم)
            IsEssential = true // کوکی ضروری (حتی اگر کاربر کوکی‌های مارکتینگ را رد کند)
        };

        // 1. تنظیم Access Token (مثلاً ۱۵ دقیقه)
        var accessOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, IsEssential = true,
            Expires = DateTime.UtcNow.AddMinutes(15) // باید با تنظیمات JwtSettings هماهنگ باشد
        };
        Response.Cookies.Append("accessToken", accessToken, accessOptions);

        // 2. تنظیم Refresh Token (مثلاً ۳۰ روز)
        var refreshOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(30)
        };
        Response.Cookies.Append("refreshToken", refreshToken, refreshOptions);
    }

    private void ForceLogoutCookies()
    {
        var deleteOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };

        Response.Cookies.Delete("accessToken", deleteOptions);
        Response.Cookies.Delete("refreshToken", deleteOptions);
    }
}