using LibraryApp.Application.Auth;
using LibraryApp.Application.Common.Interfaces;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Enums;
using LibraryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _dbContext = new AppDbContext(options);
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthService>.Instance;
    }

    [Fact]
    public async Task ValidateLoginAsync_ValidCredentials_ReturnsUser()
    {
        // Arrange
        _dbContext.ApplicationUsers.Add(new ApplicationUser 
        { 
            Id = 1, 
            Login = "testuser", 
            Password = "password123", 
            UserType = UserType.User 
        });
        await _dbContext.SaveChangesAsync();

        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.ValidateLoginAsync("testuser", "password123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("testuser", result.Login);
    }

    [Fact]
    public async Task ValidateLoginAsync_InvalidCredentials_ReturnsNull()
    {
        // Arrange
        _dbContext.ApplicationUsers.Add(new ApplicationUser 
        { 
            Id = 1, 
            Login = "testuser", 
            Password = "password123", 
            UserType = UserType.User 
        });
        await _dbContext.SaveChangesAsync();

        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.ValidateLoginAsync("testuser", "wrongpassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateLoginAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.ValidateLoginAsync("nonexistent", "password123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ValidId_ReturnsUser()
    {
        // Arrange
        _dbContext.ApplicationUsers.Add(new ApplicationUser 
        { 
            Id = 1, 
            Login = "testuser", 
            UserType = UserType.User 
        });
        await _dbContext.SaveChangesAsync();

        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("testuser", result.Login);
    }

    [Fact]
    public async Task GetUserByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        _dbContext.ApplicationUsers.Add(new ApplicationUser 
        { 
            Id = 1, 
            Login = "testuser", 
            UserType = UserType.User 
        });
        await _dbContext.SaveChangesAsync();

        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ValidApiKey_ReturnsUser()
    {
        // Arrange
        var apiKey = "test-api-key-123";
        _dbContext.ApplicationUsers.Add(new ApplicationUser 
        { 
            Id = 2, 
            Login = "apiuser", 
            UserType = UserType.API, 
            ApiKey = apiKey 
        });
        await _dbContext.SaveChangesAsync();

        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.ValidateApiKeyAsync(apiKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal(UserType.API, result.UserType);
        Assert.Equal(apiKey, result.ApiKey);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_InvalidApiKey_ReturnsNull()
    {
        // Arrange
        _dbContext.ApplicationUsers.Add(new ApplicationUser 
        { 
            Id = 2, 
            Login = "apiuser", 
            UserType = UserType.API, 
            ApiKey = "valid-key" 
        });
        await _dbContext.SaveChangesAsync();

        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.ValidateApiKeyAsync("invalid-key");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateApiKeyAsync_NullOrWhitespaceApiKey_ReturnsNull(string? apiKey)
    {
        // Arrange
        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.ValidateApiKeyAsync(apiKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WrongUserType_ReturnsNull()
    {
        // Arrange
        var apiKey = "some-key";
        _dbContext.ApplicationUsers.Add(new ApplicationUser 
        { 
            Id = 1, 
            Login = "webuser", 
            UserType = UserType.User, 
            ApiKey = apiKey 
        });
        await _dbContext.SaveChangesAsync();

        var service = new AuthService(_dbContext, _logger);

        // Act
        var result = await service.ValidateApiKeyAsync(apiKey);

        // Assert
        Assert.Null(result);
    }
}
