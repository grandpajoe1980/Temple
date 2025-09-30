# Testing Guide

This document describes the testing strategy and guidelines for the Temple project.

## Overview

Temple uses a comprehensive test-driven development approach to ensure code quality and reliability at all stages of development. The testing infrastructure is built using:

- **xUnit** - Testing framework for .NET
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing framework
- **In-Memory Database** - For fast, isolated tests
- **WebApplicationFactory** - For full HTTP pipeline testing

## Test Organization

Tests are organized in the `src/Server/Temple.Tests` project with the following structure:

```
Temple.Tests/
├── Domain/           # Unit tests for domain entities
│   ├── TenantTests.cs
│   ├── UserTests.cs
│   └── ...
├── Integration/      # Integration tests for API endpoints
│   ├── TenantIntegrationTests.cs
│   └── ...
├── Auth/            # Authentication-specific tests
│   └── AuthTests.cs
├── ApiIntegrationTests.cs  # Core API integration tests
└── SlugTests.cs     # Value object tests
```

## Test Categories

### 1. Unit Tests

Unit tests validate individual domain entities, value objects, and business logic in isolation.

**Location**: `Temple.Tests/Domain/`

**Example**:
```csharp
[Fact]
public void New_Tenant_Has_Unique_Id()
{
    var tenant1 = new Tenant();
    var tenant2 = new Tenant();
    
    Assert.NotEqual(tenant1.Id, tenant2.Id);
}
```

**Guidelines**:
- Test one thing at a time
- Use descriptive test names (e.g., `New_User_Has_Creation_Timestamp`)
- Test both happy path and edge cases
- Keep tests fast and independent

### 2. Integration Tests

Integration tests validate API endpoints, middleware, and the full HTTP request/response pipeline.

**Location**: `Temple.Tests/Integration/`

**Example**:
```csharp
[Fact]
public async Task Create_Tenant_Returns_201_Created()
{
    var client = _factory.CreateClient();
    var request = new TenantCreateRequest("New Tenant", null, null);

    var response = await client.PostAsJsonAsync("/api/v1/tenants", request);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

**Guidelines**:
- Test complete user workflows
- Validate HTTP status codes, headers, and response bodies
- Use in-memory database for isolation
- Clean up test data (handled automatically with in-memory DB)

### 3. Authentication Tests

Special category for testing authentication and authorization flows.

**Location**: `Temple.Tests/Auth/`

**Guidelines**:
- Test registration, login, token generation
- Test unauthorized access attempts
- Validate JWT token structure and claims

## Running Tests

### Run All Tests
```bash
cd src/Server
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~TenantTests"
```

### Run with Detailed Output
```bash
dotnet test --verbosity normal
```

### Run with Coverage (future)
```bash
dotnet test /p:CollectCoverage=true
```

## Writing New Tests

### 1. Create Test Class

```csharp
using Xunit;

namespace Temple.Tests.Domain;

public class YourEntityTests
{
    [Fact]
    public void Test_Description()
    {
        // Arrange
        var entity = new YourEntity();
        
        // Act
        var result = entity.SomeMethod();
        
        // Assert
        Assert.Equal(expectedValue, result);
    }
}
```

### 2. Use Theory for Multiple Test Cases

```csharp
[Theory]
[InlineData("input1", "expected1")]
[InlineData("input2", "expected2")]
public void Test_With_Multiple_Cases(string input, string expected)
{
    var result = ProcessInput(input);
    Assert.Equal(expected, result);
}
```

### 3. Integration Test Template

```csharp
public class YourIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public YourIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Your_Test()
    {
        var client = _factory.CreateClient();
        
        var response = await client.GetAsync("/api/v1/your-endpoint");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## Test Database

Tests use an in-memory database that is:
- **Isolated**: Each test class gets its own database instance
- **Fast**: No disk I/O overhead
- **Clean**: Automatically reset between test runs
- **Configured**: Set via `Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true")`

## Best Practices

### DO:
✅ Write tests before implementing features (TDD)
✅ Use descriptive test names that describe the behavior
✅ Test one logical concept per test
✅ Use the Arrange-Act-Assert pattern
✅ Keep tests fast and independent
✅ Test edge cases and error conditions
✅ Use `Theory` for parameterized tests

### DON'T:
❌ Write tests that depend on execution order
❌ Share mutable state between tests
❌ Test implementation details
❌ Use real databases or external services in unit tests
❌ Ignore failing tests
❌ Skip writing tests for "simple" code

## Continuous Integration

Tests are automatically run on every push and pull request via GitHub Actions. See `.github/workflows/test.yml` for the CI configuration.

### CI Pipeline:
1. Build the solution
2. Run all tests
3. Report test results
4. (Future) Generate code coverage report
5. (Future) Publish coverage to codecov.io

## Code Coverage

Currently, code coverage reporting is not configured but is planned for future implementation.

**Target Coverage**: 80% minimum for production code

### Coverage Workflow (planned):
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Troubleshooting

### Test Fails with "Hangfire" Error
**Solution**: Ensure `UseInMemoryDatabase` environment variable is set to "true" in test constructor.

### Test Fails with 404 Not Found
**Solution**: Verify the endpoint path uses `/api/v1/` prefix (not `/api/`).

### Tests Pass Locally but Fail in CI
**Solution**: Check for timezone, culture, or environment-specific dependencies.

## Future Improvements

- [ ] Add code coverage reporting
- [ ] Add performance/load tests
- [ ] Add E2E tests for critical workflows
- [ ] Add mutation testing
- [ ] Add contract tests for API versioning
- [ ] Add snapshot tests for API responses

## Resources

- [xUnit Documentation](https://xunit.net/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
