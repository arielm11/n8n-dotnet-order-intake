namespace OrderIntake.Api.Models;

public sealed record NewOrderLine(Guid ProductId, int Quantity, decimal UnitPrice);