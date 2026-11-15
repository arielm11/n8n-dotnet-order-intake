using System.Net.Mail;
using OrderIntake.Api.Dtos;

namespace OrderIntake.Api.Validators;

public static class CreateOrderRequestValidator
{
    private const int MaxItems = 50;

    public static Dictionary<string, string[]> Validate(CreateOrderRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        void Add(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages))
                messages = [];
                errors[key] = messages;

            messages.Add(message);
        }

        if (request.Customer is null)
        {
            Add("customer", "Customer is required.");
        }
        else
        {
            var name = request.Customer.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Add("customer.name", "Name is required.");
            }
            else if (name.Length > 100)
            {
                Add("customer.name", "Name must be at most 100 characters.");
            }

            var email = request.Customer.Email?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                Add("customer.email", "Email is required.");
            }
            else if (email.Length > 255
                || !MailAddress.TryCreate(email, out var parsed)
                || parsed.Address != email)
            {
                Add("customer.email", "Email is invalid.");
            }
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            Add("items", "At least one item is required.");
        }
        else if (request.Items.Count > MaxItems)
        {
            Add("items", $"An order can have at most {MaxItems} items.");
        }
        else
        {
            var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                if (item is null)
                {
                    Add($"items[{i}]", "Item is required.");
                    continue;
                }

                var sku = item.Sku?.Trim();
                if (string.IsNullOrEmpty(sku))
                {
                    Add($"items[{i}].sku", "SKU is required.");
                }
                else if (!seenSkus.Add(sku))
                {
                    Add($"items[{i}].sku", "Duplicate SKU. Combine the quantities in a single item.");
                }

                if (item.Quantity <= 0)
                    Add($"items[{i}].quantity", "Quantity must be greater than zero.");
            }
        }

        return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }
}