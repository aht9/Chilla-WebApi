using System.Data;
using System.Text;
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
using Chilla.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Chilla_";
        });
        
        // ثبت سرویس کش هوشمند
        services.AddScoped<ICacheService, CacheService>();

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
            
            // جاب پاکسازی (Cleanup Job)
            var cleanupJobKey = new JobKey("OutboxCleanupJob");
            q.AddJob<OutboxCleanupJob>(opts => opts.WithIdentity(cleanupJobKey));
            
            q.AddTrigger(opts => opts
                .ForJob(cleanupJobKey)
                .WithIdentity("OutboxCleanupJob-Trigger")
                // اجرای روزانه با Cron Expression (ساعت 04:00 صبح)
                .WithCronSchedule("0 0 4 * * ?"));
            
            //job پاکسازی توکن های قدیمی
            var tokenCleanupKey = new JobKey("RefreshTokenCleanupJob");
            q.AddJob<RefreshTokenCleanupJob>(opts => opts.WithIdentity(tokenCleanupKey));

            q.AddTrigger(opts => opts
                .ForJob(tokenCleanupKey)
                .WithIdentity("RefreshTokenCleanupJob-Trigger")
                // اجرای روزانه در ساعت 03:00 بامداد
                .WithCronSchedule("0 0 3 * * ?"));
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
        services.AddScoped<AppDbContextInitialiser>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ISmsSender, SmsSender>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        
        // --- [SECTION FIX]: Authentication Configuration ---
        // این بخش دلیل خطای شما بود که احتمالا جا افتاده است
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

        services.AddAuthentication(options =>
            {
                // تعریف استراتژی پیش‌فرض: همه چیز باید با JWT چک شود
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
                    ClockSkew = TimeSpan.Zero
                };
            
                // خواندن توکن از کوکی (برای امنیت بیشتر)
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.ContainsKey("accessToken"))
                        {
                            context.Token = context.Request.Cookies["accessToken"];
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        // ---------------------------------------------------
        return services;
    }
}