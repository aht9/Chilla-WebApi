using System.Data;
using Chilla.Domain.Aggregates.InvoiceAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.BackgroundJobs;
using Chilla.Infrastructure.Common;
using Chilla.Infrastructure.Persistence;
using Chilla.Infrastructure.Persistence.Interceptors;
using Chilla.Infrastructure.Persistence.Repositories;
using Chilla.Infrastructure.Persistence.Services;
using Chilla.Infrastructure.Services;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

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

            options.UseSqlServer(connectionString, sqlOptions => 
            {
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            })
            .AddInterceptors(auditInterceptor, outboxInterceptor);
        });

        // 1.1. UnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // 1.2. Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>(); // Added Registration

        // 2. Dapper & Redis
        services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));
        services.AddTransient<IDapperService, DapperService>();
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Chilla_";
        });

        // 3. Background Services (Quartz for Outbox)
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            var jobKey = new JobKey("OutboxProcessJob");
            q.AddJob<OutboxProcessJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("OutboxProcessJob-Trigger")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).RepeatForever()));
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        
        // 4. MediatR
        var applicationAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Chilla.Application");

        if (applicationAssembly != null)
        {
            services.AddMediatR(cfg => 
            {
                cfg.RegisterServicesFromAssembly(applicationAssembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            });
        }

        // 5. Auth & Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ISmsSender, SmsSender>();
        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
}