using Dapper;
using OrderIntake.Api.Data;
using OrderIntake.Api.Models;
using OrderIntake.Api.Models.Enums;

namespace OrderIntake.Api.Repositories;

public sealed class OrderRepository(SqlConnectionFactory connectionFactory)
{
    public async Task<Guid> CreateAsync(
        string customerName,
        string customerEmail,
        IReadOnlyList<NewOrderLine> lines,
        CancellationToken cancellationToken)
    {
        const string upsertCustomerSql = @"
            SET NOCOUNT ON;

            DECLARE @CustomerId UNIQUEIDENTIFIER;

            SELECT @CustomerId = Id
            FROM Customers WITH (UPDLOCK, HOLDLOCK)
            WHERE Email = @Email;

            IF @CustomerId IS NULL
                INSERT INTO Customers (Name, Email)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Email);
            ELSE
            BEGIN
                UPDATE Customers SET Name = @Name WHERE Id = @CustomerId;
                SELECT @CustomerId;
            END
            ";

        const string insertOrderSql = @"
            INSERT INTO Orders (CustomerId, Status, TotalPrice)
            OUTPUT INSERTED.Id
            VALUES (@CustomerId, @Status, @TotalPrice);
            ";

        const string insertItemSql = @"
            INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
            VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);
            ";

        var totalPrice = lines.Sum(l => l.Quantity * l.UnitPrice);

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var customerId = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            upsertCustomerSql,
            new { Name = customerName, Email = customerEmail },
            transaction,
            cancellationToken: cancellationToken));

        var orderId = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            insertOrderSql,
            new { CustomerId = customerId, Status = OrderStatus.Received, TotalPrice = totalPrice },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertItemSql,
            lines.Select(l => new { OrderId = orderId, l.ProductId, l.Quantity, l.UnitPrice }),
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return orderId;
    }

    public async Task<OrderDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.Id, o.Status, o.TotalPrice, o.CreatedAtUtc, o.RejectionReason, o.DecidedAtUtc,
                   c.Name AS CustomerName, c.Email AS CustomerEmail
            FROM Orders o
            INNER JOIN Customers c ON c.Id = o.CustomerId
            WHERE o.Id = @Id;

            SELECT p.Sku, p.Name, i.Quantity, i.UnitPrice
            FROM OrderItems i
            INNER JOIN Products p ON p.Id = i.ProductId
            WHERE i.OrderId = @Id
            ORDER BY p.Name;
            ";

        await using var connection = connectionFactory.Create();
        using var grid = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        var order = await grid.ReadSingleOrDefaultAsync<OrderRow>();
        if (order is null)
        {
            return null;
        }

        var items = (await grid.ReadAsync<OrderItemRow>()).AsList();

        return new OrderDetails(order, items);
    }

    public async Task<StatusUpdateResult> UpdateStatusAsync(
        Guid id,
        string status,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        const string updateSql = @"
            UPDATE Orders
            SET Status = @Status,
                RejectionReason = @RejectionReason,
                DecidedAtUtc = SYSUTCDATETIME()
            WHERE Id = @Id AND Status = @PendingStatus;
            ";

        const string existsSql = @"
            SELECT COUNT(1) FROM Orders WHERE Id = @Id;
            ";

        await using var connection = connectionFactory.Create();

        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            updateSql,
            new { Id = id, Status = status, RejectionReason = rejectionReason, PendingStatus = OrderStatus.Received },
            cancellationToken: cancellationToken));

        if (affectedRows == 1)
        {
            return StatusUpdateResult.Updated;
        }

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { Id = id }, cancellationToken: cancellationToken));

        return exists == 0 ? StatusUpdateResult.NotFound : StatusUpdateResult.AlreadyDecided;
    }
}