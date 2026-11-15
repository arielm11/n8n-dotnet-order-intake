namespace OrderIntake.Api.Models;

public static class OrderStatus
{
    public const string Received = "Received";
    public const string Confirmed = "Confirmed";
    public const string Rejected = "Rejected";

    public static readonly string[] Decisions = [Confirmed, Rejected];

    public static string? NormalizeDecision(string? value) =>
        Decisions.FirstOrDefault(s => string.Equals(s, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}