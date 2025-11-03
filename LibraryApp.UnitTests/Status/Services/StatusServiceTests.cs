using LibraryApp.Application.Common.Interfaces;
using LibraryApp.Application.Status.Services;
using StatusEntity = LibraryApp.Domain.Entities.Status;
using LibraryApp.Domain.Entities;
using LibraryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.UnitTests.Status.Services;

public class StatusServiceTests
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<StatusService> _logger;

    public StatusServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _dbContext = new AppDbContext(options);
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<StatusService>.Instance;
    }

    [Fact]
    public async Task GetFirstStatusAsync_WhenStatusExists_ReturnsStatusEntity()
    {
        // Arrange
        _dbContext.Statuses.Add(new StatusEntity { Id = 1, Value = "OK" });
        _dbContext.Statuses.Add(new StatusEntity { Id = 2, Value = "Test" });
        await _dbContext.SaveChangesAsync();

        var service = new StatusService(_dbContext, _logger);

        // Act
        var result = await service.GetFirstStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("OK", result.Value);
    }

    [Fact]
    public async Task GetFirstStatusAsync_WhenNoStatusExists_ReturnsStatusWithEmptyValue()
    {
        // Arrange
        var service = new StatusService(_dbContext, _logger);

        // Act
        var result = await service.GetFirstStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Value);
    }
}
