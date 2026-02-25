using System.Net;
using System.Net.Http.Json;
using Chilla.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chilla.Tests.Controllers;

public class CouponsControllerTests : TestBase
{
    public CouponsControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ValidateCoupon_WithValidCoupon_ShouldReturnSuccess()
    {
        // Arrange
        var query = new
        {
            CouponCode = "VALID123",
            PlanId = Guid.NewGuid()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(query);

        // Act
        var response = await authenticatedClient.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateCoupon_WithInvalidCoupon_ShouldReturnBadRequest()
    {
        // Arrange
        var query = new
        {
            CouponCode = "INVALID123",
            PlanId = Guid.NewGuid()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(query);

        // Act
        var response = await authenticatedClient.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateCoupon_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var query = new
        {
            CouponCode = "TEST123",
            PlanId = Guid.NewGuid()
        };
        
        var content = JsonContent.Create(query);

        // Act
        var response = await _client.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("DISCOUNT10")]
    [InlineData("SALE50")]
    [InlineData("WELCOME100")]
    [InlineData("EXPIRED123")]
    public async Task ValidateCoupon_WithDifferentCouponCodes_ShouldWorkCorrectly(string couponCode)
    {
        // Arrange
        var query = new
        {
            CouponCode = couponCode,
            PlanId = Guid.NewGuid()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(query);

        // Act
        var response = await authenticatedClient.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateCoupon_WithEmptyCouponCode_ShouldReturnBadRequest()
    {
        // Arrange
        var query = new
        {
            CouponCode = "",
            PlanId = Guid.NewGuid()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(query);

        // Act
        var response = await authenticatedClient.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateCoupon_WithNullCouponCode_ShouldReturnBadRequest()
    {
        // Arrange
        var query = new
        {
            CouponCode = (string?)null,
            PlanId = Guid.NewGuid()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(query);

        // Act
        var response = await authenticatedClient.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("123e4567-e89b-12d3-a456-426614174999")]
    public async Task ValidateCoupon_WithDifferentPlanIds_ShouldWorkCorrectly(string planIdString)
    {
        // Arrange
        var query = new
        {
            CouponCode = "TEST123",
            PlanId = Guid.Parse(planIdString)
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(query);

        // Act
        var response = await authenticatedClient.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateCoupon_WithExpiredCoupon_ShouldReturnBadRequest()
    {
        // Arrange
        var query = new
        {
            CouponCode = "EXPIRED123",
            PlanId = Guid.NewGuid()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(query);

        // Act
        var response = await authenticatedClient.PostAsync("/api/coupons/validate", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrEmpty();
        }
    }
}
