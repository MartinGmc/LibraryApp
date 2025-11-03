using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace LibraryApp.Web.Http;

public class CookieForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CookieForwardingHandler> _logger;

    public CookieForwardingHandler(IHttpContextAccessor httpContextAccessor, ILogger<CookieForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        System.Threading.CancellationToken cancellationToken)
    {
        // Forward cookies from HttpContext to the outgoing request
        var httpContext = _httpContextAccessor.HttpContext;
        var logger = _logger;
        
        if (httpContext != null && request.RequestUri != null)
        {
            logger?.LogInformation(
                "CookieForwardingHandler - Request: {Method} {Uri}, Cookies in HttpContext: {CookieCount}", 
                request.Method, request.RequestUri, httpContext.Request.Cookies.Count);
            
            // Build cookie header from Request.Cookies collection
            // This is the reliable approach that works with SameSite=None
            if (httpContext.Request.Cookies.Count > 0)
            {
                var cookieHeader = string.Join("; ", httpContext.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    logger?.LogInformation("CookieForwardingHandler - Adding cookie header with {Count} cookies", httpContext.Request.Cookies.Count);
                    request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                }
            }
        }
        else
        {
            logger?.LogWarning("CookieForwardingHandler - HttpContext is null or RequestUri is null");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

