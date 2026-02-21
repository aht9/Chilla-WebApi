using Chilla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chilla.WebApi.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<AppDbContext>();

            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                logger.LogInformation("Applying pending database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations.");
            // در محیط پروداکشن ممکن است بخواهید برنامه متوقف شود اگر دیتابیس آماده نیست
            throw; 
        }
    }
}