using System.Net;
using System.Text.Json;
using Chilla.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Chilla.WebApi.Middlewares;

public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }


    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context); 
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "خطای مدیریت نشده: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {           
        context.Response.ContentType = "application/json";
        var response = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case OtpValidationException otpEx:
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                response.Status = (int)HttpStatusCode.BadRequest;
                response.Title = "خطای اعتبارسنجی";
                response.Detail = otpEx.Message;
                break;
            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Status = (int)HttpStatusCode.NotFound;
                response.Title = "یافت نشد";
                response.Detail = exception.Message;
                break;

            default:
                // خطای ۵۰۰ برای سایر موارد
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Status = (int)HttpStatusCode.InternalServerError;
                response.Title = "خطای سمت سرور";
                response.Detail = "متاسفانه خطایی رخ داده است."; 
                break;
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}