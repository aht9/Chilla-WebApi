using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Chilla.WebApi.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddJwtToken(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.Zero // حذف تلورانس ۵ دقیقه‌ای پیش‌فرض
                };

                // Event برای خواندن توکن از کوکی (اگر فرانت‌اند توکن را در هدر نفرستد)
                options.Events = new JwtBearerEvents
                {
                    // استراتژی استخراج توکن:
                    // 1. اولویت با هدر Authorization است (برای کلاینت‌های موبایل یا Postman).
                    // 2. اگر هدر نبود، کوکی accessToken چک می‌شود (برای مرورگر وب).
                    // 3. اگر درخواست SignalR بود، کوکی یا Query String چک می‌شود.
                    OnMessageReceived = context =>
                    {
                        // نام کوکی که AccessToken را در آن ذخیره می‌کنیم
                        const string accessTokenCookieName = "accessToken";

                        // اگر توکن قبلاً در هدر بود، کاری نداریم (اولویت با هدر)
                        if (!string.IsNullOrEmpty(context.Token))
                            return Task.CompletedTask;

                        // بررسی وجود کوکی
                        if (context.Request.Cookies.ContainsKey(accessTokenCookieName))
                        {
                            context.Token = context.Request.Cookies[accessTokenCookieName];
                        }

                        // (اختیاری) پشتیبانی از SignalR که توکن را در QueryString می‌فرستد
                        // var accessToken = context.Request.Query["access_token"];
                        // if (!string.IsNullOrEmpty(accessToken))
                        // {
                        //     context.Token = accessToken;
                        // }

                        return Task.CompletedTask;
                    }
                };
            });

        // 2. Authorization
        services.AddAuthorization();
        return services;
    }
}