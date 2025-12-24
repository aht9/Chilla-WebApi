using System.Reflection;
using Chilla.Application.Common.Behaviors;
using Chilla.Application.Extensions.Services;
using Chilla.Application.Services.Interface;
using Chilla.Infrastructure.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chilla.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)); 
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // حالا این خط باید بدون خطا کار کند
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}