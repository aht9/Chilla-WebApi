using Chilla.Application.Features.Auth.Commands;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Chilla.Tests.Application.Features.Auth.Commands;

public class RequestOtpCommandTests
{
    private readonly Mock<IOtpService> _mockOtpService;
    private readonly Mock<ISmsSender> _mockSmsSender;
    private readonly Mock<ILogger<RequestOtpHandler>> _mockLogger;
    private readonly RequestOtpHandler _handler;

    public RequestOtpCommandTests()
    {
        _mockOtpService = new Mock<IOtpService>();
        _mockSmsSender = new Mock<ISmsSender>();
        _mockLogger = new Mock<ILogger<RequestOtpHandler>>();
        
        _handler = new RequestOtpHandler(
            _mockOtpService.Object,
            _mockSmsSender.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidPhoneNumber_ShouldGenerateOtpAndSendSms()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var expectedCode = "123456";
        var command = new RequestOtpCommand(phoneNumber);

        _mockOtpService
            .Setup(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", 2))
            .ReturnsAsync(expectedCode);

        _mockSmsSender
            .Setup(x => x.SendAsync(phoneNumber, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeTrue();
        result.Code.Should().Be(expectedCode);

        _mockOtpService.Verify(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", 2), Times.Once);
        _mockSmsSender.Verify(x => x.SendAsync(phoneNumber, $"کد ورود شما به چله: {expectedCode}"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOtpServiceFails_ShouldPropagateException()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var command = new RequestOtpCommand(phoneNumber);

        _mockOtpService
            .Setup(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", 2))
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

        _mockSmsSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSmsSenderFails_ShouldStillReturnResult()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var expectedCode = "123456";
        var command = new RequestOtpCommand(phoneNumber);

        _mockOtpService
            .Setup(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", 2))
            .ReturnsAsync(expectedCode);

        _mockSmsSender
            .Setup(x => x.SendAsync(phoneNumber, It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMS service unavailable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeTrue();
        result.Code.Should().Be(expectedCode);

        _mockOtpService.Verify(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", 2), Times.Once);
        _mockSmsSender.Verify(x => x.SendAsync(phoneNumber, $"کد ورود شما به چله: {expectedCode}"), Times.Once);
    }

    [Theory]
    [InlineData("09123456789")]
    [InlineData("09221234567")]
    [InlineData("+989123456789")]
    public async Task Handle_WithDifferentPhoneNumbers_ShouldWorkCorrectly(string phoneNumber)
    {
        // Arrange
        var expectedCode = "123456";
        var command = new RequestOtpCommand(phoneNumber);

        _mockOtpService
            .Setup(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", 2))
            .ReturnsAsync(expectedCode);

        _mockSmsSender
            .Setup(x => x.SendAsync(phoneNumber, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeTrue();
        result.Code.Should().Be(expectedCode);

        _mockOtpService.Verify(x => x.GenerateAndCacheOtpAsync(phoneNumber, "login", 2), Times.Once);
        _mockSmsSender.Verify(x => x.SendAsync(phoneNumber, $"کد ورود شما به چله: {expectedCode}"), Times.Once);
    }
}
