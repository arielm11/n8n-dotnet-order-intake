using OrderIntake.Api.Dtos;
using OrderIntake.Api.Models;

namespace OrderIntake.Api.Validators;

public static class UpdateOrderStatusRequestValidator
{
    private const int MaxReasonLength = 500;

    public static Dictionary<string, string[]> Validate(UpdateOrderStatusRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var status = OrderStatus.NormalizeDecision(request.Status);
        if (status is null)
        {
            errors["status"] = [$"Status must be one of: {string.Join(", ", OrderStatus.Decisions)}."];
            return errors;
        }

        var reason = request.RejectionReason?.Trim();

        if (status == OrderStatus.Rejected)
        {
            if (string.IsNullOrEmpty(reason))
            {
                errors["rejectionReason"] = ["Rejection reason is required when rejecting an order."];
            }
            else if (reason.Length > MaxReasonLength)
            {
                errors["rejectionReason"] = [$"Rejection reason must be at most {MaxReasonLength} characters."];
            }
        }
        else if (!string.IsNullOrEmpty(reason))
        {
            errors["rejectionReason"] = ["Rejection reason is only allowed when rejecting an order."];
        }

        return errors;
    }
}