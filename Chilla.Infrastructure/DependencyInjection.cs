using System.Data;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.BackgroundJobs;
using Chilla.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chilla.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. EF Core Configuration (Write Side)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions => 
            {
                // تنظیمات تاب‌آوری در برابر قطعی‌های موقت شبکه
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

        // 2. Dapper Configuration (Read Side)
        services.AddScoped<IDbConnection>(sp => 
            new SqlConnection(connectionString));

        // 3. Redis Caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Chilla_";
        });

        // 4. Background Services (Outbox)
        services.AddHostedService<OutboxProcessor>();

        // 5. Authentication Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // 6. External Services (Implementation of Interfaces defined in Application Layer)
        // services.AddTransient<IEmailSender, EmailSender>(); // To be implemented
        // services.AddTransient<ISmsSender, SmsSender>();     // To be implemented

        return services;
    }
}