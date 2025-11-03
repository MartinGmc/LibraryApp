using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LibraryApp.Application.Common.Interfaces;
using LibraryApp.Infrastructure.Persistence;

namespace LibraryApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString, sqliteOptions =>
                sqliteOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        
        // Register Seeders
        services.AddScoped<UserSeeder>();
        services.AddScoped<BookSeeder>();

        return services;
    }
}


