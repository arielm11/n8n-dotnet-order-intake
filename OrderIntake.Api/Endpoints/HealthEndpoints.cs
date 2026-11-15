using Dapper;
using Microsoft.Data.SqlClient;
using OrderIntake.Api.Data;
using OrderIntake.Api.Dtos;

namespace OrderIntake.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", CheckHealthAsync)
            .WithName("GetHealth")
            .WithTags("Health")
            .Produces<HealthResponse>(StatusCodes.Status200OK)
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> CheckHealthAsync(
        SqlConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = connectionFactory.Create();
            await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));

            return TypedResults.Ok(new HealthResponse("healthy", "ok"));
        }
        catch (SqlException ex)
        {
            loggerFactory.CreateLogger(nameof(HealthEndpoints))
                .LogError(ex, "Database health check failed");

            return TypedResults.Json(
                new HealthResponse("unhealthy", "unreachable"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}