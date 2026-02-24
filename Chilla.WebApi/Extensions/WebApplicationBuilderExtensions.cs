using System.Reflection;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Chilla.WebApi.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddAppSettingBuilder(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var configFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "ConfigurationFiles",
            builder.Environment.EnvironmentName);
        builder.Configuration.AddJsonFile(Path.Combine(configFolderPath, "appsettings.AuthSettings.json"),
            optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile(Path.Combine(configFolderPath, "appsettings.ConnectionString.json"),
            optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile(Path.Combine(configFolderPath, "appsettings.Logging.json"), optional: true,
            reloadOnChange: true);
        builder.Configuration.AddJsonFile(Path.Combine(configFolderPath, "appsettings.Serilog.json"), optional: true,
            reloadOnChange: true);
        return builder;
    }

    public static WebApplicationBuilder AddSerilogBuilder(this WebApplicationBuilder builder)
    {
        // 1. Serilog Setup (بهترین پرکتیس برای لاگینگ)
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());
        return builder;
    }

    public static WebApplicationBuilder AddSwaggerBuilder(this WebApplicationBuilder builder)
    {
        // --- [START] SWAGGER CONFIGURATION ---
        // کانفیگ حرفه‌ای Swagger با قابلیت احراز هویت
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Chilla Web API",
                Version = "v1",
                Description = "API برای مدیریت چله‌نشینی و عادات"
            });

            // تعریف امنیت (Bearer Token)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "توکن JWT خود را وارد کنید (بدون کلمه Bearer، فقط توکن)."
            });

            // اعمال امنیت روی تمام اندپوینت‌ها
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });

            // --- [START] XML COMMENTS CONFIGURATION ---
            // مسیر فایل XML تولید شده توسط بیلد پروژه را پیدا می‌کنیم
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            // اگر فایل وجود داشت، آن را به Swagger اضافه کن
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // اگر کامنت‌های پروژه‌های لایه Domain یا Application را هم می‌خواهید نشان دهید،
            // باید فایل XML آن‌ها را هم به همین شکل اضافه کنید.
            // --- [END] XML COMMENTS CONFIGURATION ---
        });
        // --- [END] SWAGGER CONFIGURATION ---

        return builder;
    }

    public static WebApplicationBuilder AddCorsBuilder(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowClient", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://chilla.ir") // آدرس کلاینت‌ها
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // برای ارسال کوکی (Refresh Token) ضروری است
            });
        });

        return builder;
    }
}