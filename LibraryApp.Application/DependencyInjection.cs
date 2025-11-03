using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using LibraryApp.Application.Auth;
using LibraryApp.Application.Books;
using LibraryApp.Application.Books.Services;
using LibraryApp.Application.Status.Services;

namespace LibraryApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Services
        services.AddScoped<IStatusService, StatusService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookLoanService, BookLoanService>();
        services.AddScoped<IIsbnValidationService, IsbnValidationService>();

        // Register FluentValidation validators from Application assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}

