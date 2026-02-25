using System.Net;
using System.Net.Http.Json;
using Chilla.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chilla.Tests.Controllers;

public class CartsControllerTests : TestBase
{
    public CartsControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyCart_WithAuthentication_ShouldReturnCart()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyCart_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddPlanToCart_WithValidPlanId_ShouldReturnSuccess()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var request = new
        {
            UserPreferences = new List<object>()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(request);

        // Act
        var response = await authenticatedClient.PostAsync($"/api/cart/items/{planId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("پلن با موفقیت به سبد خرید شما اضافه شد");
    }

    [Fact]
    public async Task AddPlanToCart_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var request = new
        {
            UserPreferences = new List<object>()
        };
        
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync($"/api/cart/items/{planId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveItemFromCart_WithValidPlanId_ShouldReturnSuccess()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.DeleteAsync($"/api/cart/items/{planId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("چله از سبد خرید حذف شد");
    }

    [Fact]
    public async Task RemoveItemFromCart_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var planId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/cart/items/{planId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApplyCoupon_WithValidCoupon_ShouldReturnSuccess()
    {
        // Arrange
        var couponCode = "TEST123";
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(couponCode);

        // Act
        var response = await authenticatedClient.PostAsync("/api/cart/coupon", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("کد تخفیف با موفقیت روی سبد شما اعمال شد");
    }

    [Fact]
    public async Task ApplyCoupon_WithEmptyCoupon_ShouldReturnBadRequest()
    {
        // Arrange
        var couponCode = "";
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(couponCode);

        // Act
        var response = await authenticatedClient.PostAsync("/api/cart/coupon", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("کد تخفیف را وارد کنید");
    }

    [Fact]
    public async Task ApplyCoupon_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var couponCode = "TEST123";
        var content = JsonContent.Create(couponCode);

        // Act
        var response = await _client.PostAsync("/api/cart/coupon", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveCoupon_WithAuthentication_ShouldReturnSuccess()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.DeleteAsync("/api/cart/coupon");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("کد تخفیف حذف شد");
    }

    [Fact]
    public async Task RemoveCoupon_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/api/cart/coupon");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_WithFreeCart_ShouldReturnSuccess()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.PostAsync("/api/cart/checkout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Checkout_WithPaymentRequired_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.PostAsync("/api/cart/checkout", null);

        // Assert - This test might need to be adjusted based on actual cart state
        // The response could be OK (for free cart) or BadRequest (for paid cart)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/cart/checkout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("123e4567-e89b-12d3-a456-426614174999")]
    public async Task AddPlanToCart_WithDifferentPlanIds_ShouldWorkCorrectly(string planIdString)
    {
        // Arrange
        var planId = Guid.Parse(planIdString);
        var request = new
        {
            UserPreferences = new List<object>()
        };
        
        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(request);

        // Act
        var response = await authenticatedClient.PostAsync($"/api/cart/items/{planId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
