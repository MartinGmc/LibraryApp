using LibraryApp.Domain.Entities;

namespace LibraryApp.Application.Auth;

public interface IAuthService
{
    Task<ApplicationUser?> ValidateLoginAsync(string login, string password, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}

