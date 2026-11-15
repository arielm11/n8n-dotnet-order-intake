namespace OrderIntake.Api.Models;

public sealed record OrderDetails(OrderRow Order, IReadOnlyList<OrderItemRow> Items);