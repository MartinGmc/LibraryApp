using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Web.Filters;

/// <summary>
/// Action filter to log ModelState validation errors before controller action execution.
/// This helps diagnose model binding issues that occur before the controller method is called.
/// </summary>
public class ModelStateLoggingFilter : IActionFilter
{
    private readonly ILogger<ModelStateLoggingFilter> _logger;

    public ModelStateLoggingFilter(ILogger<ModelStateLoggingFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
        var route = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
        var method = context.HttpContext.Request.Method;
        
        // Always log when entering action filter for debugging
        _logger.LogInformation(
            "=== ModelStateLoggingFilter ENTERED === Action: {Action}, Route: {Route}, Method: {Method}, ModelState.IsValid: {IsValid}, ErrorCount: {ErrorCount}, ActionArguments.Count: {ArgCount}",
            actionName, route, method, context.ModelState.IsValid, context.ModelState.ErrorCount, context.ActionArguments.Count);
        
        // Log action arguments to see what was bound
        foreach (var arg in context.ActionArguments)
        {
            _logger.LogInformation("ModelStateLoggingFilter - ActionArgument: {Key} = {Value} (Type: {Type})", 
                arg.Key, arg.Value?.ToString() ?? "null", arg.Value?.GetType().Name ?? "null");
        }
        
        if (!context.ModelState.IsValid)
        {
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
                
                _logger.LogWarning(
                    "=== ModelState validation failed for {Action} - Route: {Route}, Method: {Method} ===",
                    actionName, route, method);
                
                _logger.LogWarning(
                    "ModelState errors ({Count}): {Errors}",
                    errors.Count,
                    string.Join(" | ", errorDetails));
                
                // Log all ModelState keys and values
                var allKeys = string.Join(", ", context.ModelState.Keys);
                _logger.LogWarning("ModelState Keys: [{Keys}]", allKeys);
            }
        }
        else
        {
            _logger.LogInformation("ModelStateLoggingFilter - ModelState is valid for {Action}", actionName);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }
}

