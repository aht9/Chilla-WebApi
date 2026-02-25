# Chilla WebAPI Tests

This project contains comprehensive tests for the Chilla WebAPI application, following best practices for testing CQRS architecture, EF Core, and Dapper implementations.

## Test Structure

### Controllers Tests
- **AuthControllerTests**: Tests for authentication endpoints (OTP, login, logout, password reset)
- **CartsControllerTests**: Tests for shopping cart functionality
- **PlansControllerTests**: Tests for plan management and user plans
- **CouponsControllerTests**: Tests for coupon validation
- **UsersControllerTests**: Tests for user profile management
- **DashboardControllerTests**: Tests for dashboard data retrieval
- **InvoicesControllerTests**: Tests for invoice and order history

### Application Layer Tests (CQRS)
- **RequestOtpCommandTests**: Tests for OTP generation and sending
- **LoginWithOtpCommandTests**: Tests for OTP-based login flow
- **CompleteProfileCommandTests**: Tests for user profile completion

## Test Infrastructure

### Common Test Classes
- **TestApplicationFactory**: WebApplicationFactory setup with in-memory database and mocked services
- **TestBase**: Base class for controller tests with common setup and helper methods
- **MockDapperService**: Mock implementation for Dapper read operations

### Database Configuration
- Uses Entity Framework Core InMemory database for write operations
- Mock Dapper service for read operations
- Test-specific configuration in `appsettings.Testing.json`

## Key Features Tested

### Authentication Flow
- OTP request and validation
- Login with OTP and password
- Token refresh mechanism
- Profile completion
- Password reset functionality
- Logout and token revocation

### Shopping Cart System
- Adding/removing plans from cart
- Coupon application and removal
- Checkout process
- Cart retrieval for authenticated users

### Plan Management
- Active plans retrieval (public)
- User-specific plans (authenticated)
- Plan details and progress tracking
- Plan subscription and covenant signing

### User Management
- Profile retrieval
- Profile completion with validation
- User-specific data access

### Dashboard & Analytics
- Dashboard state retrieval
- User-specific metrics and progress
- Invoice and order history

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthControllerTests"

# Run tests in parallel
dotnet test --parallel
```

## Test Coverage

The tests cover:
- ✅ HTTP endpoint responses (200, 400, 401, 404, etc.)
- ✅ Authentication and authorization
- ✅ Input validation and error handling
- ✅ CQRS command and query handlers
- ✅ Database operations (EF Core)
- ✅ Service interactions (SMS, OTP, JWT)
- ✅ Edge cases and error scenarios

## Mocking Strategy

- **IOtpService**: Mocked for OTP generation and validation
- **ISmsSender**: Mocked for SMS sending operations
- **IPasswordHasher**: Mocked for password operations
- **IJwtTokenGenerator**: Mocked for token generation
- **IDapperService**: Mocked for read operations
- **IUserRepository**: Mocked for user data operations

## Configuration

### Test Settings
- JWT settings configured for testing
- In-memory database for fast test execution
- Mocked external service dependencies
- Logging set to warning level to reduce noise

### Environment
- Tests run in "Testing" environment
- Separate configuration from development/production
- Optimized for CI/CD pipeline execution

## Best Practices Applied

1. **Arrange-Act-Assert** pattern in all tests
2. **Descriptive test names** that explain the scenario
3. **Theory tests** for parameterized scenarios
4. **Proper cleanup** and disposal of resources
5. **Mock verification** to ensure service interactions
6. **Comprehensive assertion** of responses and behaviors
7. **Edge case testing** for error conditions

## Future Enhancements

- Add integration tests with real database
- Performance testing for high-load scenarios
- Security testing for authentication flows
- API contract testing with OpenAPI specifications
- Load testing for concurrent user scenarios
