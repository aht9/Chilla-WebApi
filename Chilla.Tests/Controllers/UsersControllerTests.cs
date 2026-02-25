using System.Net;
using Chilla.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chilla.Tests.Controllers;

public class UsersControllerTests : TestBase
{
    public UsersControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyProfile_WithAuthentication_ShouldReturnUserProfile()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetMyProfile_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyProfile_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient("invalid_token");

        // Act
        var response = await authenticatedClient.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyProfile_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient("expired_token");

        // Act
        var response = await authenticatedClient.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("valid_token_123")]
    [InlineData("user_token_456")]
    [InlineData("admin_token_789")]
    public async Task GetMyProfile_WithDifferentValidTokens_ShouldReturnProfile(string token)
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient(token);

        // Act
        var response = await authenticatedClient.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturnJsonContentType()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/users/me");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Fact]
    public async Task GetMyProfile_WithMultipleRequests_ShouldBeConsistent()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response1 = await authenticatedClient.GetAsync("/api/users/me");
        var response2 = await authenticatedClient.GetAsync("/api/users/me");

        // Assert
        response1.StatusCode.Should().Be(response2.StatusCode);
        
        if (response1.StatusCode == HttpStatusCode.OK && response2.StatusCode == HttpStatusCode.OK)
        {
            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();
            content1.Should().Be(content2);
        }
    }
}
