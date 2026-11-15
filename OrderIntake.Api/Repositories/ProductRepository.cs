using Dapper;
using OrderIntake.Api.Data;
using OrderIntake.Api.Models;

namespace OrderIntake.Api.Repositories;

public sealed class ProductRepository(SqlConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<ProductRow>> GetActiveAsync(CancellationToken cancellationToken)
    {
        const string sql =
            @"SELECT Id, Sku, Name, Price
            FROM Products
            WHERE IsActive = 1
            ORDER BY Name;
            ";

        await using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<ProductRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<IReadOnlyList<ProductRow>> GetActiveBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken)
    {
        const string sql =
            @"SELECT Id, Sku, Name, Price
            FROM Products
            WHERE IsActive = 1 AND Sku IN @Skus;
            ";

        await using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<ProductRow>(
            new CommandDefinition(sql, new { Skus = skus }, cancellationToken: cancellationToken));

        return rows.AsList();
    }
}