using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace LibraryApp.Web.Swagger;

public class ApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the endpoint requires API Key authentication
        var hasApiKeyAuth = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Any(attr => attr.AuthenticationSchemes == "ApiKey");

        // Check controller-level attributes
        if (!hasApiKeyAuth)
        {
            hasApiKeyAuth = context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any(attr => attr.AuthenticationSchemes == "ApiKey") ?? false;
        }

        if (hasApiKeyAuth)
        {
            // Add API Key security requirement to this operation
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        },
                        In = ParameterLocation.Header
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}

