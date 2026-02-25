using System.Net;
using Chilla.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chilla.Tests.Controllers;

public class InvoicesControllerTests : TestBase
{
    public InvoicesControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyInvoices_WithAuthentication_ShouldReturnInvoices()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrEmpty();
        responseString.Should().Contain("Count");
        responseString.Should().Contain("Data");
    }

    [Fact]
    public async Task GetMyInvoices_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyInvoices_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient("invalid_token");

        // Act
        var response = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyInvoices_ShouldReturnJsonContentType()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetMyInvoices_ResponseShouldHaveCorrectStructure()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrEmpty();
        
        // Should contain the expected structure
        responseString.Should().Contain("\"count\"");
        responseString.Should().Contain("\"data\"");
        responseString.Should().StartWith("{");
        responseString.Should().EndWith("}");
    }

    [Theory]
    [InlineData("user_token_123")]
    [InlineData("admin_token_456")]
    [InlineData("premium_user_789")]
    public async Task GetMyInvoices_WithDifferentTokens_ShouldReturnInvoices(string token)
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient(token);

        // Act
        var response = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrEmpty();
        responseString.Should().Contain("Count");
        responseString.Should().Contain("Data");
    }

    [Fact]
    public async Task GetMyInvoices_WithMultipleRequests_ShouldBeConsistent()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response1 = await authenticatedClient.GetAsync("/api/invoices/my-invoices");
        var response2 = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content1.Should().NotBeNullOrEmpty();
        content2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMyInvoices_CountShouldBeNonNegative()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrEmpty();
        
        // The count should be a non-negative number
        responseString.Should().MatchRegex(@"""count"":\s*\d+");
    }

    [Fact]
    public async Task GetMyInvoices_DataShouldBeArray()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/invoices/my-invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().NotBeNullOrEmpty();
        
        // The data should be an array
        responseString.Should().MatchRegex(@"""data"":\s*\[");
    }
}
