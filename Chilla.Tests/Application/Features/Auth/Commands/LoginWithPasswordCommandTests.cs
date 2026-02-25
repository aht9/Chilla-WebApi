using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chilla.Application.Features.Auth.Commands;
using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Aggregates.UserAggregate;
using FluentValidation;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Common;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Chilla.Tests.Application.Features.Auth.Commands;

public class LoginWithPasswordCommandTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenGenerator> _jwtGeneratorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly LoginWithPasswordHandler _handler;

    public LoginWithPasswordCommandTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtGeneratorMock = new Mock<IJwtTokenGenerator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();

        _handler = new LoginWithPasswordHandler(
            _userRepositoryMock.Object,
            _jwtGeneratorMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnAuthResult_WhenValidCredentials()
    {
        // Arrange
        var command = new LoginWithPasswordCommand("testuser", "ValidPassword123", "192.168.1.1");

        var user = new Chilla.Domain.Aggregates.UserAggregate.User("John", "Doe", "testuser", "+989123456789", "test@example.com");
        user.SetPassword("ValidPassword123");

        _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("testuser", default))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(hasher => hasher.VerifyPassword("ValidPassword123", user.PasswordHash))
            .Returns(true);

        _jwtGeneratorMock.Setup(generator => generator.GenerateAccessToken(user.Id, user.Username, It.IsAny<IEnumerable<string>>()))
            .Returns("test-token");

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test-token");
        result.RefreshToken.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Phone.Should().Be("+989123456789");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidCredentialsException_WhenUserNotFound()
    {
        // Arrange
        var command = new LoginWithPasswordCommand("nonexistent", "ValidPassword123", "192.168.1.1");

        _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("nonexistent", default))
            .ReturnsAsync((Chilla.Domain.Aggregates.UserAggregate.User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidCredentialsException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var command = new LoginWithPasswordCommand("testuser", "InvalidPassword", "192.168.1.1");

        var user = new Chilla.Domain.Aggregates.UserAggregate.User("John", "Doe", "testuser", "+989123456789", "test@example.com");
        user.SetPassword("ValidPassword123");

        _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("testuser", default))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(hasher => hasher.VerifyPassword("InvalidPassword", user.PasswordHash))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidCredentialsException_WhenUserIsLockedOut()
    {
        // Arrange
        var command = new LoginWithPasswordCommand("lockeduser", "ValidPassword123", "192.168.1.1");

        var user = new Chilla.Domain.Aggregates.UserAggregate.User("John", "Doe", "testuser", "+989123456789", "test@example.com");
        user.SetPassword("ValidPassword123");
        user.LockoutUntil(DateTimeOffset.UtcNow.AddDays(1));

        _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("lockeduser", default))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(hasher => hasher.VerifyPassword("ValidPassword123", user.PasswordHash))
            .Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidCredentialsException_WhenUserIsInactive()
    {
        // Arrange
        var command = new LoginWithPasswordCommand("inactiveuser", "ValidPassword123", "192.168.1.1");

        var user = new Chilla.Domain.Aggregates.UserAggregate.User("John", "Doe", "testuser", "+989123456789", "test@example.com");
        user.SetPassword("ValidPassword123");
        user.ToggleActivity(false);

        _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync("inactiveuser", default))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(hasher => hasher.VerifyPassword("ValidPassword123", user.PasswordHash))
            .Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenUsernameIsNull()
    {
        // Arrange
        var command = new LoginWithPasswordCommand(null, "ValidPassword123", "192.168.1.1");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenPasswordIsNull()
    {
        // Arrange
        var command = new LoginWithPasswordCommand("testuser", null, "192.168.1.1");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenIpAddressIsNull()
    {
        // Arrange
        var command = new LoginWithPasswordCommand("testuser", "ValidPassword123", null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, default));
    }
}