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
            .WriteTo.File("logs/boot-log-.txt", rollingInterval: RollingInterval.Day) 
            .CreateBootstrapLogger();
        try
        {
            Log.Information("Starting Web Application...");
            var builder = WebApplication.CreateBuilder(args);
            builder.AddAppSettingBuilder();
            builder.AddSerilogBuilder();
            builder.AddSwaggerBuilder();
            builder.AddCorsBuilder();
            // 2. Add Services (Layers)
            // لایه Infrastructure (شامل دیتابیس، Auth، جاب‌های پس‌زمینه)
            builder.Services.AddInfrastructure(builder.Configuration);
            // لایه Application (شامل MediatR، ولیدیشن‌ها)
            builder.Services.AddApplication(builder.Configuration);
            builder.Services.AddTransient<GlobalExceptionHandler>();
            builder.Services.AddHttpContextAccessor();
            // 3. API Services
            builder.Services.AddControllers();
            
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
            
            app.UseSerilogRequestLogging();
            app.UseCors("AllowClient");
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

