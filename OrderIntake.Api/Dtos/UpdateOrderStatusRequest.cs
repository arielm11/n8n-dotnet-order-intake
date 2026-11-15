namespace OrderIntake.Api.Dtos;

public sealed record UpdateOrderStatusRequest(string? Status, string? RejectionReason);