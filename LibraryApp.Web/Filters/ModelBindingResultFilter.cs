using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Web.Filters;

/// <summary>
/// Result filter to log ModelState errors after action execution
/// </summary>
public class ModelBindingResultFilter : IResultFilter
{
    private readonly ILogger<ModelBindingResultFilter> _logger;

    public ModelBindingResultFilter(ILogger<ModelBindingResultFilter> logger)
    {
        _logger = logger;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
        var route = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
        
        _logger.LogInformation(
            "=== ModelBindingResultFilter - OnResultExecuting === Action: {Action}, Route: {Route}, ResultType: {ResultType}",
            actionName, route, context.Result?.GetType().Name ?? "null");
        
        // Log ModelState even if action wasn't executed
        if (context.ModelState != null)
        {
            _logger.LogInformation(
                "ModelBindingResultFilter - ModelState.IsValid: {IsValid}, ErrorCount: {ErrorCount}",
                context.ModelState.IsValid, context.ModelState.ErrorCount);
            
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => new
                    {
                        Property = x.Key,
                        ErrorMessage = e.ErrorMessage,
                        Exception = e.Exception?.Message
                    }))
                    .ToList();
                
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelBindingResultFilter - ModelState Error: Property={Property}, Error={Error}, Exception={Exception}",
                        error.Property, error.ErrorMessage, error.Exception ?? "None");
                }
            }
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // No action needed after result execution
    }
}

