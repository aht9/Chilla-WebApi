using System.Data;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.BackgroundJobs;
using Chilla.Infrastructure.Persistence;
using Chilla.Infrastructure.Persistence.Interceptors;
using Chilla.Infrastructure.Persistence.Repositories;
using Chilla.Infrastructure.Services;
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
        // ثبت اینترسپتورها به صورت Scoped برای استفاده در DbContext
        services.AddScoped<ConvertDomainEventsToOutboxInterceptor>();
        services.AddScoped<AuditableEntityInterceptor>();

        // 1. EF Core Configuration (Write Side)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var outboxInterceptor = sp.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>();
            var auditInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();

            options.UseSqlServer(connectionString, sqlOptions => 
            {
                // تنظیمات تاب‌آوری در برابر قطعی‌های موقت شبکه
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            })
            // افزودن اینترسپتورها برای Auditing و Outbox Pattern
            .AddInterceptors(auditInterceptor, outboxInterceptor);
        });

        // 1.1. UnitOfWork Registration
        // نکته مهم: IUnitOfWork را به همان کانتکس ایجاد شده وصل می‌کنیم تا تراکنش واحد حفظ شود
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // 1.2. Repositories Registration
        // ثبت ریپوزیتوری‌های استاندارد
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        // 2. Dapper & Redis (Read Side & Caching)
        services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Chilla_";
        });

        // 3. Background Services (Outbox Processor)
        // سرویس پس‌زمینه برای پردازش پیام‌های Outbox
        services.AddQuartz(q =>
        {
            // استفاده از DI کانتینر مایکروسافت برای ساختن اینستنس جاب‌ها
            q.UseMicrosoftDependencyInjectionJobFactory();

            // تعریف کلید یکتا برای جاب پردازش Outbox
            var jobKey = new JobKey("OutboxProcessJob");

            // ثبت جاب
            q.AddJob<OutboxProcessJob>(opts => opts.WithIdentity(jobKey));

            // زمان‌بندی اجرا: هر 5 ثانیه یک‌بار
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("OutboxProcessJob-Trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(5)
                    .RepeatForever()));
        });

        // افزودن سرویس هوست Quartz که مدیریت اجرای جاب‌ها در پس‌زمینه را بر عهده دارد
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        
        
        // 4. Authentication & Security Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOtpService, OtpService>();
        
        // 5. Notification Services
        // پیاده‌سازی‌های ساده شده برای لاگ کردن (در آینده با سرویس‌های واقعی جایگزین می‌شوند)
        services.AddScoped<ISmsSender, SmsSender>();
        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
}