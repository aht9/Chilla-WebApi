using Chilla.Infrastructure;
using Serilog;

namespace Chilla.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // تنظیم Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/chilla-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog(); // جایگزین کردن لاگر پیش‌فرض
        
        
        // Add Layers
        // builder.Services.AddApplication(); // MediatR, Validators, etc.
        builder.Services.AddInfrastructure(builder.Configuration); // DbContext, Jwt, BackgroundJobs
        
        
        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}