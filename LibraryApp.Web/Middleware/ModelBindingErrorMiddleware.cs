using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LibraryApp.Web.Middleware;

public class ModelBindingErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ModelBindingErrorMiddleware> _logger;

    public ModelBindingErrorMiddleware(RequestDelegate next, ILogger<ModelBindingErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only intercept API routes
        if (context.Request.Path.StartsWithSegments("/api") && 
            context.Request.Method == "POST" && 
            context.Request.ContentType?.Contains("application/json") == true)
        {
            // Check if endpoint was matched
            var endpoint = context.GetEndpoint();
            var endpointName = endpoint?.DisplayName ?? "No endpoint matched";
            _logger.LogInformation("ModelBindingErrorMiddleware - Endpoint: {Endpoint}, Route: {Route}", endpointName, context.Request.Path);
            
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            _logger.LogInformation("ModelBindingErrorMiddleware - After next middleware. StatusCode: {StatusCode}, ResponseBodyLength: {Length}, HasStarted: {HasStarted}, Endpoint: {Endpoint}", 
                context.Response.StatusCode, responseBody.Length, context.Response.HasStarted, endpointName);

            // If status is 400 and no content was written, log and add error response
            if (context.Response.StatusCode == 400 && responseBody.Length == 0)
            {
                _logger.LogWarning("ModelBindingErrorMiddleware - 400 response with no body detected. Writing error response.");
                
                var errorResponse = new
                {
                    status = 400,
                    title = "Bad Request",
                    detail = "Request validation failed. Please check your input.",
                    errors = new Dictionary<string, string[]>
                    {
                        ["general"] = new[] { "Unable to bind request. Please verify the JSON format and property names." }
                    }
                };

                responseBody.SetLength(0);
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(responseBody, errorResponse);
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            
            context.Response.Body = originalBodyStream;
        }
        else
        {
            await _next(context);
        }
    }
}
