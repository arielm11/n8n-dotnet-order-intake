namespace OrderIntake.Api.Models;

public sealed record OrderRow(
    Guid Id,
    string Status,
    decimal TotalPrice,
    DateTime CreatedAtUtc,
    string? RejectionReason,
    DateTime? DecidedAtUtc,
    string CustomerName,
    string CustomerEmail);