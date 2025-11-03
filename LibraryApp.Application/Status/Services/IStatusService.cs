using StatusEntity = LibraryApp.Domain.Entities.Status;

namespace LibraryApp.Application.Status.Services;

public interface IStatusService
{
    Task<StatusEntity> GetFirstStatusAsync(CancellationToken cancellationToken = default);
}

