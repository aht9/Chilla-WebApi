using System.Data;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.BackgroundJobs;
using Chilla.Infrastructure.Persistence;
using Chilla.Infrastructure.Persistence.Interceptors;
using Chilla.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chilla.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 0. Interceptors
        services.AddScoped<ConvertDomainEventsToOutboxInterceptor>();
        services.AddScoped<AuditableEntityInterceptor>();

        // 1. EF Core Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var outboxInterceptor = sp.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>();
            var auditInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();

            options.UseSqlServer(connectionString)
                .AddInterceptors(auditInterceptor, outboxInterceptor); // ترتیب مهم است
        });

        // 2. Dapper & Redis
        services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Chilla_";
        });

        // 3. Background Services
        services.AddHostedService<OutboxProcessor>();

        // 4. Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ISmsSender, SmsSender>(); // Simple Logger Implementation

        return services;
    }
}