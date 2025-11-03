using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Enums;

namespace LibraryApp.Infrastructure.Persistence;

public class UserSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(AppDbContext context, ILogger<UserSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if users already exist
            if (await _context.ApplicationUsers.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Users already exist in database. Skipping seed.");
                return;
            }

            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    Login = "User1",
                    Password = "User1pass",
                    UserType = UserType.User,
                    ApiKey = string.Empty,
                    Permissions = Permissions.Standard
                },
                new ApplicationUser
                {
                    Id = 2,
                    Login = "User2",
                    Password = "User2pass",
                    UserType = UserType.User,
                    ApiKey = string.Empty,
                    Permissions = Permissions.Standard
                },
                new ApplicationUser
                {
                    Id = 3,
                    Login = "API1",
                    Password = "Api1pass",
                    UserType = UserType.API,
                    ApiKey = string.Empty, // API key can be set later if needed
                    Permissions = Permissions.Standard
                },
                new ApplicationUser
                {
                    Id = 4,
                    Login = "admin1",
                    Password = "admin1pass",
                    UserType = UserType.User,
                    ApiKey = string.Empty,
                    Permissions = Permissions.Elevated
                }
            };

            await _context.ApplicationUsers.AddRangeAsync(users, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} users successfully", 4);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding users");
            throw;
        }
    }
}

