using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LibraryApp.Application.Auth;
using LibraryApp.Domain.Enums;

namespace LibraryApp.Web.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IAuthService _authService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IAuthService authService)
        : base(options, logger, encoder, clock)
    {
        _authService = authService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogInformation("ApiKeyAuthenticationHandler: Starting authentication for path: {Path}, method: {Method}", 
            Request.Path, Request.Method);

        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            Logger.LogWarning("ApiKeyAuthenticationHandler: No API key header found - returning Fail");
            return AuthenticateResult.Fail("API key was not provided");
        }

        var providedApiKey = apiKeyHeaderValues.ToString();
        Logger.LogInformation("ApiKeyAuthenticationHandler: API key received (length: {Length})", providedApiKey?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            Logger.LogWarning("ApiKeyAuthenticationHandler: API key is empty - returning Fail");
            return AuthenticateResult.Fail("API key was not provided");
        }

        try
        {
            var user = await _authService.ValidateApiKeyAsync(providedApiKey, Context.RequestAborted);

            if (user == null)
            {
                Logger.LogWarning("ApiKeyAuthenticationHandler: INVALID API key provided - returning Fail");
                return AuthenticateResult.Fail("Invalid API key");
            }
            
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Login ?? string.Empty),
                new("UserType", user.UserType.ToString()),
                new("Permissions", user.Permissions.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("ApiKeyAuthenticationHandler: Success - returning Success with user {UserId}", user.Id);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while authenticating API key");
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Logger.LogWarning("ApiKeyAuthenticationHandler: HandleChallengeAsync called");
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append("WWW-Authenticate", "ApiKey");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Logger.LogWarning("ApiKeyAuthenticationHandler: HandleForbiddenAsync called");
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}

