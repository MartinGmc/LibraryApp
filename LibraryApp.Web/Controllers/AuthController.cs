using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryApp.Application.Auth;

namespace LibraryApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Login) || string.IsNullOrWhiteSpace(loginDto.Password))
        {
            _logger.LogWarning("Login failed - empty credentials");
            return BadRequest(new { message = "Login and password are required" });
        }

        var user = await _authService.ValidateLoginAsync(loginDto.Login, loginDto.Password, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed - invalid credentials for user {Login}", loginDto.Login);
            return Unauthorized(new { message = "Invalid login credentials" });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login ?? string.Empty),
            new Claim("UserType", user.UserType.ToString()),
            new Claim("Permissions", user.Permissions.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {Login} logged in successfully - UserId: {UserId}, UserType: {UserType}", 
            user.Login, user.Id, user.UserType);

        return Ok(new { message = "Login successful", userId = user.Id });
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return Ok(new { message = "Logout successful" });
    }
}

