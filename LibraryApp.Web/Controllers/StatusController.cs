using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryApp.Application.Status.Services;
using LibraryApp.Domain.Entities;
using StatusEntity = LibraryApp.Domain.Entities.Status;

namespace LibraryApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IStatusService _statusService;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IStatusService statusService, ILogger<StatusController> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    [HttpGet("first")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<string>> GetFirstStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetFirstStatus endpoint called");
        StatusEntity status = await _statusService.GetFirstStatusAsync(cancellationToken);
        return new JsonResult(status.Value);
    }
}

