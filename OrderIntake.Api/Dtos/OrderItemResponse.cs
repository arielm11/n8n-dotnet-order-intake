namespace OrderIntake.Api.Dtos;

public sealed record OrderItemResponse(string Sku, string Name, int Quantity, decimal UnitPrice, decimal LineTotal);