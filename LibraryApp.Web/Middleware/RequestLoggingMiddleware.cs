using System.Text;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Web.Middleware;

/// <summary>
/// Middleware to log incoming API requests, especially for debugging model binding issues
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log for API endpoints to avoid noise
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            // Enable buffering so we can read the body multiple times
            context.Request.EnableBuffering();
            
            // Log request details
            var method = context.Request.Method;
            var path = context.Request.Path + context.Request.QueryString;
            var contentType = context.Request.ContentType ?? "unknown";
            
            // Try to read request body for POST/PUT/PATCH
            string? requestBody = null;
            if (method is "POST" or "PUT" or "PATCH")
            {
                try
                {
                    var originalBodyStream = context.Request.Body;
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read request body for logging");
                }
            }
            
            // Log authentication status
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var authType = context.User?.Identity?.AuthenticationType ?? "None";
            var userName = context.User?.Identity?.Name ?? "Anonymous";
            
            // Log cookies (especially auth cookie)
            var cookieCount = context.Request.Cookies.Count;
            var hasAuthCookie = context.Request.Cookies.ContainsKey(".AspNetCore.Cookies");
            var cookieNames = string.Join(", ", context.Request.Cookies.Keys);
            
            // Log headers that might be relevant
            var hasApiKeyHeader = context.Request.Headers.ContainsKey("X-API-Key");
            var hasAuthorizationHeader = context.Request.Headers.ContainsKey("Authorization");
            
            _logger.LogInformation(
                "API Request: {Method} {Path}, ContentType: {ContentType}, Body: {RequestBody}",
                method, path, contentType, requestBody ?? "(none)");
            
            _logger.LogInformation(
                "API Request Auth: IsAuthenticated: {IsAuthenticated}, AuthType: {AuthType}, UserName: {UserName}, " +
                "Cookies: {CookieCount} (HasAuthCookie: {HasAuthCookie}), CookieNames: [{CookieNames}], " +
                "HasApiKeyHeader: {HasApiKeyHeader}, HasAuthorizationHeader: {HasAuthorizationHeader}",
                isAuthenticated, authType, userName, cookieCount, hasAuthCookie, cookieNames, 
                hasApiKeyHeader, hasAuthorizationHeader);
        }
        
        await _next(context);
        
        // Log response status after processing
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var hasContentLength = context.Response.ContentLength > 0;
            var contentType = context.Response.ContentType ?? "none";
            var hasStarted = context.Response.HasStarted;
            _logger.LogInformation(
                "API Response: {Method} {Path} - Status: {StatusCode}, ContentType: {ContentType}, ContentLength: {ContentLength}, HasStarted: {HasStarted}",
                context.Request.Method, 
                context.Request.Path + context.Request.QueryString,
                context.Response.StatusCode,
                contentType,
                context.Response.ContentLength ?? 0,
                hasStarted);
        }
    }
}

