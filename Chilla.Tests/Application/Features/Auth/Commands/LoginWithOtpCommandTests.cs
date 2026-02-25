using System.Collections.Generic;
using Chilla.Application.Features.Auth.Commands;
using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Chilla.Tests.Application.Features.Auth.Commands;

public class LoginWithOtpCommandTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IOtpService> _mockOtpService;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<AppDbContext> _mockDbContext;
    private readonly Mock<ILogger<LoginWithOtpHandler>> _mockLogger;
    private readonly LoginWithOtpHandler _handler;

    public LoginWithOtpCommandTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockOtpService = new Mock<IOtpService>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _mockLogger = new Mock<ILogger<LoginWithOtpHandler>>();
        
        _handler = new LoginWithOtpHandler(
            _mockUserRepository.Object,
            _mockOtpService.Object,
            _mockJwtGenerator.Object,
            _mockUnitOfWork.Object,
            _mockDbContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidOtp_ShouldReturnAuthResult()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var code = "123456";
        var ipAddress = "192.168.1.1";
        var command = new LoginWithOtpCommand(phoneNumber, code, ipAddress);

        var user = new Chilla.Domain.Aggregates.UserAggregate.User(phoneNumber);
        user.CompleteProfile("John", "Doe", "johndoe", "john@example.com");

        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(phoneNumber, code, "login"))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(x => x.GetByPhoneNumberAsync(phoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockJwtGenerator
            .Setup(x => x.GenerateAccessToken(user.Id, user.Username, It.IsAny<IEnumerable<string>>()))
            .Returns("access_token");

        _mockJwtGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.IsProfileCompleted.Should().BeTrue();
        result.Message.Should().Be("ورود موفق.");

        _mockOtpService.Verify(x => x.ValidateOtpAsync(phoneNumber, code, "login"), Times.Once);
        _mockJwtGenerator.Verify(x => x.GenerateAccessToken(user.Id, user.Username, It.IsAny<IEnumerable<string>>()), Times.Once);
        _mockJwtGenerator.Verify(x => x.GenerateRefreshToken(), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidOtp_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var code = "999999";
        var ipAddress = "192.168.1.1";
        var command = new LoginWithOtpCommand(phoneNumber, code, ipAddress);

        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(phoneNumber, code, "login"))
            .ReturnsAsync(false);

        _mockOtpService
            .Setup(x => x.IncrementOtpFailureCountAsync(phoneNumber))
            .ReturnsAsync(1);

        _mockUserRepository
            .Setup(x => x.GetByPhoneNumberAsync(phoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _mockOtpService.Verify(x => x.ValidateOtpAsync(phoneNumber, code, "login"), Times.Once);
        _mockOtpService.Verify(x => x.IncrementOtpFailureCountAsync(phoneNumber), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewUser_ShouldCreateUserAndReturnAuthResult()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var code = "123456";
        var ipAddress = "192.168.1.1";
        var command = new LoginWithOtpCommand(phoneNumber, code, ipAddress);

        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(phoneNumber, code, "login"))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(x => x.GetByPhoneNumberAsync(phoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockJwtGenerator
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns("access_token");

        _mockJwtGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.IsProfileCompleted.Should().BeFalse();
        result.Message.Should().Be("ورود موفق. لطفا پروفایل خود را تکمیل کنید.");

        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBlockedUser_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var code = "999999";
        var ipAddress = "192.168.1.1";
        var command = new LoginWithOtpCommand(phoneNumber, code, ipAddress);

        var user = new Chilla.Domain.Aggregates.UserAggregate.User(phoneNumber);
        // Simulate blocked user (assuming there's a way to block a user)

        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(phoneNumber, code, "login"))
            .ReturnsAsync(false);

        _mockOtpService
            .Setup(x => x.IncrementOtpFailureCountAsync(phoneNumber))
            .ReturnsAsync(5); // Assuming 5 failures lead to block

        _mockUserRepository
            .Setup(x => x.GetByPhoneNumberAsync(phoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _mockOtpService.Verify(x => x.ValidateOtpAsync(phoneNumber, code, "login"), Times.Once);
        _mockOtpService.Verify(x => x.IncrementOtpFailureCountAsync(phoneNumber), Times.Once);
    }

    [Theory]
    [InlineData("09123456789", "192.168.1.1")]
    [InlineData("09221234567", "10.0.0.1")]
    [InlineData("+989123456789", "127.0.0.1")]
    public async Task Handle_WithDifferentPhoneNumbersAndIpAddresses_ShouldWorkCorrectly(string phoneNumber, string ipAddress)
    {
        // Arrange
        var code = "123456";
        var command = new LoginWithOtpCommand(phoneNumber, code, ipAddress);

        var user = new Chilla.Domain.Aggregates.UserAggregate.User(phoneNumber);

        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(phoneNumber, code, "login"))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(x => x.GetByPhoneNumberAsync(phoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockJwtGenerator
            .Setup(x => x.GenerateAccessToken(user.Id, user.Username, It.IsAny<IEnumerable<string>>()))
            .Returns("access_token");

        _mockJwtGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");

        _mockOtpService.Verify(x => x.ValidateOtpAsync(phoneNumber, code, "login"), Times.Once);
    }
}
