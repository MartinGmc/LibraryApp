using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace LibraryApp.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new AuthenticationState(httpContext.User));
        }
        
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

