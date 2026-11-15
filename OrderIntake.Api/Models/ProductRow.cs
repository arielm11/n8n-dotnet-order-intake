namespace OrderIntake.Api.Models;

public sealed record ProductRow(Guid Id, string Sku, string Name, decimal Price);