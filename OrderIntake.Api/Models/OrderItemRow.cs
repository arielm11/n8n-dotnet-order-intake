namespace OrderIntake.Api.Models;

public sealed record OrderItemRow(string Sku, string Name, int Quantity, decimal UnitPrice);