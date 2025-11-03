using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using LibraryApp.Application;
using LibraryApp.Infrastructure;
using LibraryApp.Infrastructure.Persistence;
using LibraryApp.Web.Services;
using LibraryApp.Web.Swagger;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LibraryApp.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Determine log directory based on environment
        // Azure App Service uses /home/site/data which is persistent
        // Local development uses logs/ in the project directory
        var logDirectory = "/home/site/data/logs";
        var logPath = "/home/site/data/logs/log-.txt";
        
        // Check if running in Azure (by checking if /home/site/data exists)
        if (!Directory.Exists("/home/site/data"))
        {
            // Local development - use local logs directory
            logDirectory = "logs";
            logPath = "logs/log-.txt";
        }
        
        // Ensure log directory exists
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // Configure Serilog - read from configuration first, then override file path
        // This ensures we respect appsettings but use the correct path
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .CreateLogger();

        builder.Host.UseSerilog();
        
        // Log startup information immediately
        Log.Information("Application starting - Log directory: {LogDirectory}, Log path: {LogPath}", logDirectory, logPath);

        // Add services to the container
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        // Add Infrastructure (EF Core SQLite)
        builder.Services.AddInfrastructure(builder.Configuration);

        // Add Application (MediatR, FluentValidation)
        builder.Services.AddApplication();

        // Add API controllers for REST endpoints with JSON configuration
        builder.Services.AddControllers(options =>
        {
            // Add ModelState logging filter to diagnose binding issues
            options.Filters.Add<Filters.ModelStateLoggingFilter>();
            options.Filters.Add<Filters.ModelBindingResultFilter>();
        })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Customize the automatic 400 response for invalid model state
                options.InvalidModelStateResponseFactory = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("ApiController.Validation");
                    
                    // CRITICAL: Check if response has already started - if so, we can't write to it
                    if (context.HttpContext.Response.HasStarted)
                    {
                        logger.LogWarning("InvalidModelStateResponseFactory - Response already started, cannot write body");
                        return new Microsoft.AspNetCore.Mvc.BadRequestResult();
                    }
                    
                    var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
                    var route = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                    var method = context.HttpContext.Request.Method;
                    
                    logger.LogWarning(
                        "=== InvalidModelStateResponseFactory TRIGGERED === Action: {Action}, Route: {Route}, Method: {Method}, Response.HasStarted: {HasStarted}, ModelState.ErrorCount: {ErrorCount}",
                        actionName, route, method, context.HttpContext.Response.HasStarted, context.ModelState.ErrorCount);
                    
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => new
                        {
                            Property = x.Key,
                            ErrorMessage = e.ErrorMessage,
                            Exception = e.Exception?.Message,
                            AttemptedValue = x.Value.AttemptedValue
                        }))
                        .ToList();
                    
                    if (errors.Any())
                    {
                        var errorDetails = errors.Select(e => 
                            $"Property: '{e.Property}', Error: '{e.ErrorMessage}', AttemptedValue: '{e.AttemptedValue}', Exception: {e.Exception ?? "None"}")
                            .ToList();
                        
                        logger.LogWarning(
                            "InvalidModelStateResponseFactory - Errors ({Count}): {Errors}",
                            errors.Count,
                            string.Join(" | ", errorDetails));
                        
                        // Log all ModelState keys
                        var allKeys = string.Join(", ", context.ModelState.Keys);
                        logger.LogWarning("InvalidModelStateResponseFactory - All ModelState Keys: [{Keys}]", allKeys);
                    }
                    else
                    {
                        logger.LogWarning("InvalidModelStateResponseFactory - ModelState has errors but error list is empty. ErrorCount: {ErrorCount}",
                            context.ModelState.ErrorCount);
                    }
                    
                    logger.LogWarning(
                        "ApiController automatic validation failed for {Action} - Route: {Route}, Errors: {Errors}",
                        actionName,
                        route,
                        string.Join("; ", errors.Select(e => $"{e.Property}: {e.ErrorMessage}")));
                    
                    // Return detailed error response
                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = 400,
                        Title = "Validation Error",
                        Detail = "One or more validation errors occurred.",
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    };
                    
                    var errorDict = new Dictionary<string, string[]>();
                    foreach (var error in errors)
                    {
                        if (!errorDict.ContainsKey(error.Property))
                        {
                            errorDict[error.Property] = Array.Empty<string>();
                        }
                        var existingErrors = errorDict[error.Property].ToList();
                        existingErrors.Add(error.ErrorMessage);
                        errorDict[error.Property] = existingErrors.ToArray();
                    }
                    
                    problemDetails.Extensions["errors"] = errorDict;
                    
                    // Ensure content type is set to JSON
                    context.HttpContext.Response.ContentType = "application/json";
                    
                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problemDetails);
                };
                
                // Handle cases where model binding fails completely (before InvalidModelStateResponseFactory)
                options.SuppressModelStateInvalidFilter = false; // Ensure ModelState validation runs
            })
            .AddJsonOptions(options =>
            {
                // Configure JSON serialization - DTOs use lowercase property names, so don't use camelCase policy
                // PropertyNameCaseInsensitive allows matching properties regardless of case
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.WriteIndented = false;
            });

        // Add Swagger/OpenAPI support
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Library API",
                Version = "v1",
                Description = "API for Library App"
            });
            
            // Add API Key authentication support
            c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "API Key authentication using X-API-Key header",
                Name = "X-API-Key",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });
            
            // Add operation filter to apply API Key security requirement to endpoints with ApiKey authentication
            c.OperationFilter<ApiKeyOperationFilter>();
        });

        // Register CookieForwardingHandler as a transient service (required for AddHttpMessageHandler)
        builder.Services.AddTransient<Http.CookieForwardingHandler>();

        // Add HttpClient for Blazor components with cookie forwarding
        // Don't set BaseAddress here - set it when creating the client from scoped context
        // Note: HTTP request logging is now handled by browser-side fetch API interceptor in httpLogger.js
        // CookieContainer is disabled because it doesn't support SameSite=None properly
        // CookieForwardingHandler uses manual Cookie header instead
        builder.Services.AddHttpClient("BlazorServer")
            .AddHttpMessageHandler<Http.CookieForwardingHandler>();
        
        // Register a scoped HttpClient that uses the named client factory and sets BaseAddress
        builder.Services.AddScoped(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            
            var httpClient = httpClientFactory.CreateClient("BlazorServer");
            httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
            
            return httpClient;
        });

        // Add HttpContextAccessor for authentication state provider
        builder.Services.AddHttpContextAccessor();

        // Configure Data Protection to persist keys (required for Azure - cookies won't work after app restart without this)
        var isAzure = Directory.Exists("/home/site/data");
        var keysDirectory = isAzure ? "/home/site/data/keys" : "keys";
        
        if (!Directory.Exists(keysDirectory))
        {
            Directory.CreateDirectory(keysDirectory);
            Log.Information("Created Data Protection keys directory: {KeysDirectory}", keysDirectory);
        }
        
        // Log Data Protection key directory info
        Log.Information("Data Protection - Keys directory: {KeysDirectory}, Exists: {Exists}, IsAzure: {IsAzure}", 
            keysDirectory, Directory.Exists(keysDirectory), isAzure);
        
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
            .SetApplicationName("LibraryApp")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        
        // Log after Data Protection is configured
        Log.Information("Data Protection configured - Application: LibraryApp, Key lifetime: 90 days");

        // Configure Authentication - Cookie for User type, API Key for API type
        builder.Services.AddAuthentication(options =>
            {
                // Default scheme is Cookie, but allow API Key as alternative
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/Login";
                options.LogoutPath = "/Logout";
                options.Cookie.HttpOnly = true;
                // In production (Azure), always use Secure cookies (HTTPS only)
                // In development, allow HTTP
                options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
                    ? CookieSecurePolicy.Always 
                    : CookieSecurePolicy.SameAsRequest;
                // In Azure App Service behind a proxy/load balancer, we need SameSite=None with Secure
                // Lax doesn't work for cross-site requests even when behind the same proxy
                options.Cookie.SameSite = builder.Environment.IsProduction()
                    ? SameSiteMode.None  // Required for Azure App Service behind proxy
                    : SameSiteMode.Lax;
                // Don't set domain - let browser handle it (works with any subdomain in Azure)
                // Explicitly set path to root to ensure cookie is available everywhere
                options.Cookie.Path = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnSigningIn = context =>
                {
                    context.Properties.IsPersistent = true;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = context =>
                {
                    string cookieName = options.Cookie.Name ?? ".AspNetCore.Cookies";
                    bool hasCookie = context.Request.Cookies.ContainsKey(cookieName);
                    bool isAuth = context.Principal?.Identity?.IsAuthenticated ?? false;
                    var path = context.HttpContext.Request.Path;
                    var method = context.HttpContext.Request.Method;
                    
                    Log.Information(
                        "OnValidatePrincipal - Path: {Path}, Method: {Method}, HasCookie: {HasCookie}, IsAuthenticated: {IsAuthenticated}, CookieName: {CookieName}",
                        path, method, hasCookie, isAuth, cookieName);
                    
                    // Only log errors
                    if (hasCookie && !isAuth)
                    {
                        var cookieValue = context.Request.Cookies[cookieName];
                        var cookieLength = cookieValue?.Length ?? 0;
                        Log.Warning(
                            "=== Cookie validation failed - cookie present but not authenticated. Path: {Path}, CookieLength: {CookieLength}, CookieName: {CookieName}. Possible causes: Data Protection key mismatch, expired/corrupted cookie, or cookie format issue. ===",
                            path, cookieLength, cookieName);
                        context.RejectPrincipal();
                    }
                    
                    return Task.CompletedTask;
                };
                options.Events.OnSignedIn = context =>
                {
                    string userName = context.Principal?.Identity?.Name ?? "Unknown";
                    Log.Information("User {UserName} signed in successfully", userName);
                    return Task.CompletedTask;
                };
                options.Events.OnSigningOut = context =>
                {
                    string userName = context.HttpContext.User?.Identity?.Name ?? "Unknown";
                    Log.Information("User {UserName} is signing out", userName);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToLogin = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    var method = context.HttpContext.Request.Method;
                    Log.Warning(
                        "OnRedirectToLogin triggered - Path: {Path}, Method: {Method}, OriginalPath: {OriginalPath}, RedirectUri: {RedirectUri}",
                        path, method, context.RedirectUri, "/Login");
                    context.RedirectUri = "/Login";
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    string userName = context.HttpContext.User?.Identity?.Name ?? "Unknown";
                    var path = context.HttpContext.Request.Path;
                    var method = context.HttpContext.Request.Method;
                    Log.Warning(
                        "OnRedirectToAccessDenied - Path: {Path}, Method: {Method}, UserName: {UserName}, RedirectUri: {RedirectUri}",
                        path, method, userName, "/Login");
                    context.RedirectUri = "/Login";
                    return Task.CompletedTask;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, Authentication.ApiKeyAuthenticationHandler>(
                "ApiKey", 
                options => 
                {
                    // API Key scheme is only used when explicitly specified in [Authorize] attribute
                    // It won't be auto-challenged because default challenge scheme is Cookie
                });

        // Add Authorization - default policy uses only Cookie authentication
        // API Key authentication is only used when explicitly specified in [Authorize] attribute
        builder.Services.AddAuthorization(options =>
        {
            // Default policy uses only Cookie authentication
            // Endpoints that need API Key should explicitly specify it: [Authorize(AuthenticationSchemes = "ApiKey")]
            options.DefaultPolicy = new AuthorizationPolicyBuilder(
                    CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        var app = builder.Build();

        // Apply database migrations at startup
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                
                // Ensure data directory exists (important for Azure App Service)
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                Log.Information("Connection string: {ConnectionString}", 
                    connectionString?.Replace("Data Source=", "Data Source=***") ?? "Not configured");
                
                if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("/home/site/data"))
                {
                    var dataPath = "/home/site/data";
                    if (!Directory.Exists(dataPath))
                    {
                        Directory.CreateDirectory(dataPath);
                        Log.Information("Created directory: {DataPath}", dataPath);
                    }
                    else
                    {
                        Log.Information("Directory already exists: {DataPath}", dataPath);
                    }
                }
                
                // Get all migrations (applied and pending)
                var allMigrations = context.Database.GetMigrations().ToList();
                var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                
                Log.Information("=== Database Migration Process Started ===");
                Log.Information("Total migrations defined: {Count}", allMigrations.Count);
                Log.Information("Already applied migrations: {Count}", appliedMigrations.Count);
                Log.Information("Pending migrations: {Count}", pendingMigrations.Count);
                
                if (appliedMigrations.Any())
                {
                    Log.Information("Applied migrations:");
                    foreach (var migration in appliedMigrations)
                    {
                        Log.Information("  ✓ {MigrationName}", migration);
                    }
                }
                
                // Only apply migrations if there are pending ones
                if (pendingMigrations.Any())
                {
                    Log.Information("Pending migrations to apply:");
                    foreach (var migration in pendingMigrations)
                    {
                        Log.Information("  → {MigrationName}", migration);
                    }
                    
                    // Apply migrations
                    Log.Information("Applying {Count} pending migration(s)...", pendingMigrations.Count);
                    context.Database.Migrate();
                    
                    // Verify migrations were applied
                    var appliedAfter = context.Database.GetAppliedMigrations().ToList();
                    var newlyApplied = appliedAfter.Count - appliedMigrations.Count;
                    
                    if (newlyApplied > 0)
                    {
                        Log.Information("Successfully applied {NewlyApplied} migration(s). Total applied: {TotalApplied}", 
                            newlyApplied, appliedAfter.Count);
                        Log.Information("Newly applied migrations:");
                        var newMigrations = appliedAfter.Except(appliedMigrations).ToList();
                        foreach (var migration in newMigrations)
                        {
                            Log.Information("  ✓ {MigrationName}", migration);
                        }
                    }
                    else
                    {
                        Log.Warning("Migrate() was called but no new migrations were applied. This may indicate a race condition or migration state mismatch.");
                    }
                }
                else
                {
                    Log.Information("No pending migrations - database is already up to date");
                    Log.Information("Skipping Migrate() call to avoid unnecessary database operations");
                }
                
                Log.Information("=== Database Migration Process Completed ===");

                // Seed users
                Log.Information("=== User Seeding Process Started ===");
                var userSeeder = services.GetRequiredService<UserSeeder>();
                await userSeeder.SeedUsersAsync();
                Log.Information("=== User Seeding Process Completed ===");
                
                // Seed books
                Log.Information("=== Book Seeding Process Started ===");
                var bookSeeder = services.GetRequiredService<BookSeeder>();
                await bookSeeder.SeedBooksAsync();
                Log.Information("=== Book Seeding Process Completed ===");
                
                // Note: Logs are automatically flushed to disk every 1 second (configured in Serilog setup)
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An error occurred while migrating the database. Application will not start.");
                // Re-throw to prevent application from starting with invalid database state
                throw;
            }
        }

        // Configure global exception handling
        app.UseExceptionHandler("/Error");
        // Configure status code pages to skip API routes (they return JSON errors)
        // For API routes, we want JSON responses from InvalidModelStateResponseFactory
        app.UseStatusCodePages(context =>
        {
            // Skip status code pages for API routes - let controllers handle JSON responses
            if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
            {
                // For API routes, do nothing - let the controller's JSON response pass through
                return Task.CompletedTask;
            }
            
            // Only write status code page for non-API routes if response hasn't been written yet
            if (!context.HttpContext.Response.HasStarted)
            {
                context.HttpContext.Response.ContentType = "text/plain";
                var statusCode = context.HttpContext.Response.StatusCode;
                var statusDescription = statusCode switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    404 => "Not Found",
                    500 => "Internal Server Error",
                    _ => "Error"
                };
                return context.HttpContext.Response.WriteAsync($"Status Code: {statusCode}; {statusDescription}");
            }
            
            return Task.CompletedTask;
        });

        // Configure the HTTP request pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // Enable Swagger in all environments (including production)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Library API v1");
            c.RoutePrefix = "swagger"; // Swagger UI will be available at /swagger
        });

        // CRITICAL: Configure ForwardedHeaders FIRST (before UseHttpsRedirection and UseRouting)
        // This ensures that HTTPS is correctly detected when behind Azure's proxy/load balancer
        // ForwardedHeaders MUST be early in the pipeline to process X-Forwarded-Proto header
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            RequireHeaderSymmetry = false,
            ForwardLimit = 1 // Only trust the first proxy (Azure's load balancer)
        };
        
        // In Azure App Service, the proxy IP can vary, but we trust the forwarded headers
        // Clear the lists and the middleware will still process headers (warnings are expected)
        forwardedHeadersOptions.KnownProxies.Clear();
        forwardedHeadersOptions.KnownNetworks.Clear();
        
        // Only use ForwardedHeaders in production (Azure) - in development, we don't need it
        if (app.Environment.IsProduction())
        {
            Log.Information("ForwardedHeaders middleware enabled for production environment");
            app.UseForwardedHeaders(forwardedHeadersOptions);
        }
        else
        {
            Log.Information("ForwardedHeaders middleware disabled (development environment)");
        }

        // Add request logging middleware early in the pipeline (but after forwarding headers)
        app.UseMiddleware<Middleware.RequestLoggingMiddleware>();
        
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();
        
        // Add model binding error middleware AFTER routing so it can see which endpoint was matched
        app.UseMiddleware<Middleware.ModelBindingErrorMiddleware>();

        app.MapRazorPages();
        app.MapControllers();
        
        // Configure Blazor Hub - authentication is handled via cookies and HttpContext
        // SignalR connections inherit authentication from the HTTP request that established them
        app.MapBlazorHub();
        
        app.MapFallbackToPage("/_Host");

        try
        {
            Log.Information("Starting web application");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
