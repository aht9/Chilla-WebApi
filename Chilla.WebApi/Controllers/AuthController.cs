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
    [HttpGet("request-otp/{phoneNumber}")]
    public async Task<IActionResult> RequestOtp(string phoneNumber)
    {
        var command = new RequestOtpCommand(phoneNumber);
        var result = await _mediator.Send(command);
        return Ok(new { message = $" کد تایید با موفقیت ارسال شد. برای ورود کد {result.Code} را وارد کنید" });
    }

    [HttpPost("login-otp")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginWithOtp([FromBody] LoginOtpRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";


        var command = new LoginWithOtpCommand(request.PhoneNumber, request.Code, ipAddress);
        var result = await _mediator.Send(command);

        SetTokenCookies(result.AccessToken, result.RefreshToken);

        return Ok(new LoginResponseDto(result.IsProfileCompleted, result.Message ?? "ورود موفق."));
    }

    // ----------------------------------------------------------------
    // 2. ورود با رمز عبور (سناریوی جایگزین)
    // ----------------------------------------------------------------
    [HttpPost("login-password")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LoginWithPassword([FromBody] LoginPasswordRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        var command = new LoginWithPasswordCommand(request.Username, request.Password, ipAddress);
        var result = await _mediator.Send(command);

        SetTokenCookies(result.AccessToken, result.RefreshToken);

        return Ok(new LoginResponseDto(result.IsProfileCompleted, result.Message ?? "ورود موفق."));
    }

    // ----------------------------------------------------------------
    // 3. تمدید توکن (Refresh Token Rotation)
    // ----------------------------------------------------------------
    [HttpGet("refresh-token")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
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

            return Ok(new LoginResponseDto(result.IsProfileCompleted, "تمدید موفقیت‌آمیز."));
        }
        catch (Exception)
        {
            ForceLogoutCookies();
            return Unauthorized(new { message = "نشست کاربری منقضی شده است." });
        }
    }

    // ----------------------------------------------------------------
    // 4. تکمیل پروفایل (کاربران جدید)
    // ----------------------------------------------------------------
    [HttpPost("complete-profile")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileRequest request)
    {
        var command = new CompleteProfileCommand(
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
    [HttpGet("logout")]
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

    [HttpGet("forgot-password/{phoneNumber}")]
    public async Task<IActionResult> ForgotPassword(string phoneNumber)
    {
        var command = new ForgotPasswordCommand(phoneNumber);
        var result = await _mediator.Send(command);
        return Ok(new
            { message = $"در صورت وجود حساب کاربری، کد تایید ارسال شد. برای ورود کد {result.Code} را وارد کنید" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(
            request.PhoneNumber,
            request.Code,
            request.NewPassword,
            request.ConfirmNewPassword);

        await _mediator.Send(command);

        return Ok(new { message = "رمز عبور با موفقیت تغییر کرد. لطفاً با رمز جدید وارد شوید." });
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