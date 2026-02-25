using System.Net;
using System.Net.Http.Json;
using Chilla.Application.Features.Auth.DTOs;
using Chilla.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace Chilla.Tests.Controllers;

public class AuthControllerTests : TestBase
{
    public AuthControllerTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RequestOtp_WithValidPhoneNumber_ShouldReturnSuccessMessage()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var expectedCode = "123456";
        
        SetupOtpService(phoneNumber, expectedCode);
        SetupSmsSender();

        // Act
        var response = await _client.GetAsync($"/api/auth/request-otp/{phoneNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<object>();
        content.Should().NotBeNull();
        
        // The response should contain the OTP code in the message (for testing purposes)
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain(expectedCode);
    }

    [Fact]
    public async Task LoginWithOtp_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var loginRequest = new LoginOtpRequest("09123456789", "123456");
        
        // Setup mocks for the login flow
        SetupOtpService(loginRequest.PhoneNumber, loginRequest.Code);

        var content = JsonContent.Create(loginRequest);

        // Act
        var response = await _client.PostAsync("/api/auth/login-otp", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginResponse.Should().NotBeNull();
        loginResponse!.IsProfileCompleted.Should().BeTrue().Or.BeFalse(); // Can be either based on user state
        loginResponse.Message.Should().NotBeNullOrEmpty();
        
        // Check that cookies are set
        response.Headers.Should().ContainKey("Set-Cookie");
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken"));
        cookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task LoginWithOtp_WithInvalidCode_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginOtpRequest("09123456789", "999999");
        
        // Setup mock to return false for invalid code
        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(loginRequest.PhoneNumber, loginRequest.Code, "login"))
            .ReturnsAsync(false);

        var content = JsonContent.Create(loginRequest);

        // Act
        var response = await _client.PostAsync("/api/auth/login-otp", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginWithPassword_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var loginRequest = new LoginPasswordRequest("testuser", "password123");
        var hashedPassword = "hashed_password";
        
        SetupPasswordHasher(loginRequest.Password, hashedPassword);

        var content = JsonContent.Create(loginRequest);

        // Act
        var response = await _client.PostAsync("/api/auth/login-password", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginResponse.Should().NotBeNull();
        loginResponse!.IsProfileCompleted.Should().BeTrue().Or.BeFalse();
        loginResponse.Message.Should().NotBeNullOrEmpty();
        
        // Check that cookies are set
        response.Headers.Should().ContainKey("Set-Cookie");
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken"));
        cookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        
        // Setup cookies
        _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");

        // Act
        var response = await _client.GetAsync("/api/auth/refresh-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginResponse.Should().NotBeNull();
        loginResponse!.Message.Should().Contain("تمدید موفقیت‌آمیز");
        
        // Check that new cookies are set
        response.Headers.Should().ContainKey("Set-Cookie");
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken"));
        cookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task RefreshToken_WithoutRefreshToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/refresh-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("توکن یافت نشد");
    }

    [Fact]
    public async Task CompleteProfile_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var completeProfileRequest = new CompleteProfileRequest(
            "John",
            "Doe",
            "johndoe",
            "john@example.com",
            "password123"
        );

        var authenticatedClient = CreateAuthenticatedClient();
        var content = JsonContent.Create(completeProfileRequest);

        // Act
        var response = await authenticatedClient.PostAsync("/api/auth/complete-profile", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("پروفایل با موفقیت تکمیل شد");
    }

    [Fact]
    public async Task CompleteProfile_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var completeProfileRequest = new CompleteProfileRequest(
            "John",
            "Doe",
            "johndoe",
            "john@example.com",
            "password123"
        );

        var content = JsonContent.Create(completeProfileRequest);

        // Act
        var response = await _client.PostAsync("/api/auth/complete-profile", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithAuthentication_ShouldReturnSuccess()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var authenticatedClient = CreateAuthenticatedClient();
        authenticatedClient.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");

        // Act
        var response = await authenticatedClient.GetAsync("/api/auth/logout");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("خروج با موفقیت انجام شد");
        
        // Check that cookies are deleted
        response.Headers.Should().ContainKey("Set-Cookie");
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("accessToken") && c.Contains("01 Jan 1970"));
        cookies.Should().Contain(c => c.Contains("refreshToken") && c.Contains("01 Jan 1970"));
    }

    [Fact]
    public async Task ForgotPassword_WithValidPhoneNumber_ShouldReturnSuccessMessage()
    {
        // Arrange
        var phoneNumber = "09123456789";
        var expectedCode = "654321";
        
        SetupOtpService(phoneNumber, expectedCode);
        SetupSmsSender();

        // Act
        var response = await _client.GetAsync($"/api/auth/forgot-password/{phoneNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain(expectedCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var resetRequest = new ResetPasswordRequest(
            "09123456789",
            "654321",
            "newpassword123",
            "newpassword123"
        );

        SetupOtpService(resetRequest.PhoneNumber, resetRequest.Code);
        var content = JsonContent.Create(resetRequest);

        // Act
        var response = await _client.PostAsync("/api/auth/reset-password", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseString = await response.Content.ReadAsStringAsync();
        responseString.Should().Contain("رمز عبور با موفقیت تغییر کرد");
    }

    [Fact]
    public async Task ResetPassword_WithMismatchedPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var resetRequest = new ResetPasswordRequest(
            "09123456789",
            "654321",
            "newpassword123",
            "differentpassword"
        );

        var content = JsonContent.Create(resetRequest);

        // Act
        var response = await _client.PostAsync("/api/auth/reset-password", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
