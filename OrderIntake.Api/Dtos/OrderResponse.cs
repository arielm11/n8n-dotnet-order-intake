namespace OrderIntake.Api.Dtos;

public sealed record OrderResponse(
    Guid Id,
    string Status,
    CustomerResponse Customer,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Total,
    DateTime CreatedAtUtc,
    string? RejectionReason,
    DateTime? DecidedAtUtc);