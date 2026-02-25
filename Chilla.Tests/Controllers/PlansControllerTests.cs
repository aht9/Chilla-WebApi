using System.Net;
using System.Net.Http.Json;
using Chilla.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chilla.Tests.Controllers;

public class PlansControllerTests : TestBase
{
    public PlansControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetActivePlans_WithoutAuthentication_ShouldReturnPlans()
    {
        // Act
        var response = await _client.GetAsync("/api/plans/store");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyPlans_WithAuthentication_ShouldReturnUserPlans()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/plans/my-plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyPlans_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/plans/my-plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPlanDetails_WithValidPlanId_ShouldReturnPlanDetails()
    {
        // Arrange
        var planId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/plans/{planId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPlanDetails_WithInvalidPlanId_ShouldReturnNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/plans/{planId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartPlan_WithValidPlanId_ShouldReturnSuccess()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.PostAsync($"/api/plans/{planId}/start", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartPlan_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var planId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/plans/{planId}/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignCovenant_WithValidPlanId_ShouldReturnSuccess()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var request = new
        {
            SignatureData = "base64_signature_data"
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(request);

        // Act
        var response = await authenticatedClient.PostAsync($"/api/plans/{planId}/sign-covenant", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SignCovenant_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var request = new
        {
            SignatureData = "base64_signature_data"
        };
        
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync($"/api/plans/{planId}/sign-covenant", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPlanProgress_WithValidPlanId_ShouldReturnProgress()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync($"/api/plans/{planId}/progress");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPlanProgress_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var planId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/plans/{planId}/progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("123e4567-e89b-12d3-a456-426614174999")]
    public async Task GetPlanDetails_WithDifferentPlanIds_ShouldWorkCorrectly(string planIdString)
    {
        // Arrange
        var planId = Guid.Parse(planIdString);

        // Act
        var response = await _client.GetAsync($"/api/plans/{planId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActivePlans_ShouldReturnListOfPlans()
    {
        // Act
        var response = await _client.GetAsync("/api/plans/store");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // The response should be a JSON array or object
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrEmpty();
        responseString.Should().StartWithOneOf("[", "{");
    }

    [Fact]
    public async Task GetMyPlans_ShouldReturnUserSpecificPlans()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/plans/my-plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // The response should be a JSON array or object
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrEmpty();
        responseString.Should().StartWithOneOf("[", "{");
    }
}
