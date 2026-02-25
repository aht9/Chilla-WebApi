using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using System.Net.Http.Headers;
using Xunit;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Services;

namespace Chilla.Tests.Common;

public abstract class TestBase : IClassFixture<TestApplicationFactory>, IDisposable
{
    protected readonly TestApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly Mock<IOtpService> _mockOtpService;
    protected readonly Mock<ISmsSender> _mockSmsSender;
    protected readonly Mock<IPasswordHasher> _mockPasswordHasher;
    protected readonly ServiceProvider _serviceProvider;

    protected TestBase(TestApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Setup common mocks
        _mockOtpService = new Mock<IOtpService>();
        _mockSmsSender = new Mock<ISmsSender>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        // Configure service provider for dependency injection in tests
        var services = new ServiceCollection();
        services.AddSingleton(_mockOtpService.Object);
        services.AddSingleton(_mockSmsSender.Object);
        services.AddSingleton(_mockPasswordHasher.Object);
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    protected HttpClient CreateAuthenticatedClient(string token = "test-token")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected void SetupOtpService(string phoneNumber, string expectedCode)
    {
        _mockOtpService
            .Setup(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", It.IsAny<int>()))
            .ReturnsAsync(expectedCode);

        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(phoneNumber, expectedCode, "login"))
            .ReturnsAsync(true);
    }

    protected void SetupPasswordHasher(string password, string hashedPassword)
    {
        _mockPasswordHasher
            .Setup(x => x.HashPassword(password))
            .Returns(hashedPassword);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, hashedPassword))
            .Returns(true);
    }

    protected void SetupSmsSender()
    {
        _mockSmsSender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    public virtual void Dispose()
    {
        _client?.Dispose();
        _serviceProvider?.Dispose();
    }
}
