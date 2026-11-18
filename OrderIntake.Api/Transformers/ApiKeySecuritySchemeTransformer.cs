using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OrderIntake.Api.Filters;

namespace OrderIntake.Api.Transformers;

public sealed class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "ApiKey";
    private const string ProtectedPathPrefix = "/orders";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = ApiKeyEndpointFilter.HeaderName,
            In = ParameterLocation.Header,
            Description = "API key required for order endpoints."
        };

        foreach (var (path, pathItem) in document.Paths)
        {
            if (!path.StartsWith(ProtectedPathPrefix, StringComparison.OrdinalIgnoreCase)
                || pathItem.Operations is null)
            {
                continue;
            }

            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
                });
            }
        }

        return Task.CompletedTask;
    }
}