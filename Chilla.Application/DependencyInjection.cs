using Chilla.Application.Common;
using Chilla.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chilla.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Added Password Hasher
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}