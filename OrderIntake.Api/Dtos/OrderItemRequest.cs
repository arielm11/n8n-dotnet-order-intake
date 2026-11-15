namespace OrderIntake.Api.Dtos;

public sealed record OrderItemRequest(string? Sku, int Quantity);