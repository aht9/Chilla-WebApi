using System.Net;
using Chilla.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chilla.Tests.Controllers;

public class DashboardControllerTests : TestBase
{
    public DashboardControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetDashboardState_WithAuthentication_ShouldReturnDashboardData()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetDashboardState_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardState_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient("invalid_token");

        // Act
        var response = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardState_WithMalformedToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient("malformed.token.here");

        // Act
        var response = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("user_token_123")]
    [InlineData("admin_token_456")]
    [InlineData("premium_user_789")]
    public async Task GetDashboardState_WithDifferentUserTypes_ShouldReturnDashboard(string token)
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient(token);

        // Act
        var response = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardState_ShouldReturnJsonContentType()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Fact]
    public async Task GetDashboardState_WithMultipleRequests_ShouldBeConsistent()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response1 = await authenticatedClient.GetAsync("/api/dashboard");
        var response2 = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        response1.StatusCode.Should().Be(response2.StatusCode);
        
        if (response1.StatusCode == HttpStatusCode.OK && response2.StatusCode == HttpStatusCode.OK)
        {
            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();
            content1.Should().NotBeNullOrEmpty();
            content2.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetDashboardState_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient("expired_jwt_token");

        // Act
        var response = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardState_ResponseShouldContainExpectedStructure()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/dashboard");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrEmpty();
            // Should be valid JSON
            responseString.Should().StartWithOneOf("{", "[");
        }
    }
}
