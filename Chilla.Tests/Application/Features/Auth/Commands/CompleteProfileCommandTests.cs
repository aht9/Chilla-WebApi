using Chilla.Application.Features.Auth.Commands;
using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Chilla.Tests.Application.Features.Auth.Commands;

public class CompleteProfileCommandTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<CompleteProfileHandler>> _mockLogger;
    private readonly CompleteProfileHandler _handler;

    public CompleteProfileCommandTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<CompleteProfileHandler>>();
        
        _handler = new CompleteProfileHandler(
            _mockUserRepository.Object,
            _mockUnitOfWork.Object,
            _mockPasswordHasher.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCompleteUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteProfileCommand("John", "Doe", "johndoe", "john@example.com", "password123");
        
        var user = new User("09123456789");
        
        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.HashPassword("password123"))
            .Returns("hashed_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserRepository.Verify(x => x.Update(user), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        user.IsProfileCompleted().Should().BeTrue();
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Username.Should().Be("johndoe");
        user.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteProfileCommand("John", "Doe", "johndoe", "john@example.com", "password123");

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _mockUserRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Doe", "johndoe", "john@example.com", "password123")]
    [InlineData("John", "", "johndoe", "john@example.com", "password123")]
    [InlineData("John", "Doe", "", "john@example.com", "password123")]
    public async Task Handle_WithInvalidData_ShouldThrowValidationException(
        string firstName, string lastName, string username, string email, string password)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteProfileCommand(firstName, lastName, username, email, password);
        var user = new User("09123456789");

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        // This would typically be handled by FluentValidation, but we'll test the handler behavior
        await Assert.ThrowsAnyAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDuplicateUsername_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteProfileCommand("John", "Doe", "existinguser", "john@example.com", "password123");
        var user = new User("09123456789");

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.IsUsernameTakenAsync("existinguser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkFails_ShouldPropagateException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteProfileCommand("John", "Doe", "johndoe", "john@example.com", "password123");
        var user = new User("09123456789");

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        _mockUserRepository.Verify(x => x.Update(user), Times.Once);
    }

    [Theory]
    [InlineData("John", "Doe", "johndoe123", "john.doe@example.com", "Password123!")]
    [InlineData("Jane", "Smith", "janesmith", "jane.smith@company.com", "SecurePass456")]
    public async Task Handle_WithVariousValidInputs_ShouldWorkCorrectly(
        string firstName, string lastName, string username, string email, string password)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteProfileCommand(firstName, lastName, username, email, password);
        var user = new User("09123456789");

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(password))
            .Returns("hashed_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserRepository.Verify(x => x.Update(user), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        user.IsProfileCompleted().Should().BeTrue();
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
    }

    [Fact]
    public async Task Handle_WithoutCurrentUserId_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var command = new CompleteProfileCommand("John", "Doe", "johndoe", "john@example.com", "password123");

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUserRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
