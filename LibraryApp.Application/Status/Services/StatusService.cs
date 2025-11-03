using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryApp.Application.Common.Interfaces;
using StatusEntity = LibraryApp.Domain.Entities.Status;

namespace LibraryApp.Application.Status.Services;

public class StatusService : IStatusService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<StatusService> _logger;

    public StatusService(IAppDbContext dbContext, ILogger<StatusService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StatusEntity> GetFirstStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _dbContext.Statuses
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // If no status found, return a Status with empty Value (per requirement #5)
            if (status == null)
            {
                return new StatusEntity { Value = string.Empty };
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the first status");
            throw;
        }
    }
}

