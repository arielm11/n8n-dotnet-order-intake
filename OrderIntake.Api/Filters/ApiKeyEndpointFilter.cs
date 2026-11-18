using System.Security.Cryptography;
using System.Text;

namespace OrderIntake.Api.Filters;

public sealed class ApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expectedKey = configuration["ApiKey"];
        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expectedKey) || !KeysMatch(expectedKey, providedKey))
        {
            return TypedResults.Problem(
                title: "Invalid or missing API key.",
                detail: $"Send a valid key in the {HeaderName} header.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }

    private static bool KeysMatch(string expected, string provided) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
}