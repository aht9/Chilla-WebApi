using Chilla.Application;
using Chilla.Infrastructure;
using Chilla.Infrastructure.Persistence;
using Chilla.WebApi.Extensions;
using Chilla.WebApi.Middlewares;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Chilla.WebApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/boot-log-.txt", rollingInterval: RollingInterval.Day) // لاگ جداگانه برای بوت
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Web Application...");
            var builder = WebApplication.CreateBuilder(args);

            // 1. Serilog Setup (بهترین پرکتیس برای لاگینگ)
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            // 2. Add Services (Layers)
            // لایه Infrastructure (شامل دیتابیس، Auth، جاب‌های پس‌زمینه)
            builder.Services.AddInfrastructure(builder.Configuration);
            // لایه Application (شامل MediatR، ولیدیشن‌ها)
            builder.Services.AddApplication(builder.Configuration);
            builder.Services.AddTransient<GlobalExceptionHandler>();
            builder.Services.AddHttpContextAccessor();
            
            // 3. API Services
            builder.Services.AddControllers();

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
            });
            // --- [END] SWAGGER CONFIGURATION ---

            // تنظیمات CORS (حیاتی برای ارتباط با فرانت‌اند)
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


            var app = builder.Build();
            
            // Configure Pipeline
            app.UseMiddleware<GlobalExceptionHandler>();
            app.UseMiddleware<IpRateLimitingMiddleware>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                using (var scope = app.Services.CreateScope())
                {
                    var initialiser = scope.ServiceProvider.GetRequiredService<AppDbContextInitialiser>();
                    await initialiser.InitialiseAsync(); // ساخت دیتابیس
                    await initialiser.SeedAsync();       // پر کردن نقش‌ها و دسترسی‌ها
                    
                }
                await app.ApplyMigrationsAsync();

                app.UseSwagger();
                app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chilla API v1"); });
            }
            
            // مدیریت درخواست‌ها
            app.UseSerilogRequestLogging(); // لاگ کردن درخواست‌های HTTP تمیز

            app.UseCors("AllowClient");

            // امنیت (ترتیب مهم است: اول احراز هویت، بعد دسترسی)
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();


            Log.Information("Starting Chilla Web API...");
            await app.RunAsync();
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}

