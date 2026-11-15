namespace OrderIntake.Api.Dtos;

public sealed record CreateOrderRequest(CustomerRequest? Customer, List<OrderItemRequest?>? Items);