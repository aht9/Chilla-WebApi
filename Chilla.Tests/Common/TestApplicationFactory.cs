using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Chilla.Infrastructure.Persistence;
using Chilla.Infrastructure.Persistence.Services;
using Chilla.Domain.Common;
using System;
using System.Linq;

namespace Chilla.Tests.Common;

public class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Remove the existing Dapper service registration if exists
            var dapperDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDapperService));
            if (dapperDescriptor != null)
            {
                services.Remove(dapperDescriptor);
            }

            // Add mock Dapper service for testing
            services.AddScoped<IDapperService, MockDapperService>();

            // Create the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();

            // Seed test data if needed
            // SeedTestData(db);
        });

        builder.UseEnvironment("Testing");
    }
}
