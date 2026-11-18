using OrderIntake.Api.Dtos;
using OrderIntake.Api.Filters;
using OrderIntake.Api.Models;
using OrderIntake.Api.Models.Enums;
using OrderIntake.Api.Repositories;
using OrderIntake.Api.Validators;

namespace OrderIntake.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders")
            .WithTags("Orders")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetOrderAsync)
            .WithName("GetOrder")
            .Produces<OrderResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/status", UpdateOrderStatusAsync)
            .WithName("UpdateOrderStatus")
            .Produces<OrderResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        ProductRepository productRepository,
        OrderRepository orderRepository,
        CancellationToken cancellationToken)
    {
        var errors = CreateOrderRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var customerName = request.Customer!.Name!.Trim();
        var customerEmail = request.Customer.Email!.Trim().ToLowerInvariant();
        var requestedItems = request.Items!
            .Select(i => (Sku: i!.Sku!.Trim(), i.Quantity))
            .ToList();

        var products = await productRepository.GetActiveBySkusAsync(
            requestedItems.Select(i => i.Sku).ToList(),
            cancellationToken);

        var productsBySku = products.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);

        var unavailableSkus = requestedItems
            .Where(i => !productsBySku.ContainsKey(i.Sku))
            .Select(i => i.Sku)
            .ToList();

        if (unavailableSkus.Count > 0)
        {
            return TypedResults.Problem(
                title: "Some products are unavailable.",
                detail: "One or more SKUs do not exist or are no longer sold.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: new Dictionary<string, object?> { ["unavailableSkus"] = unavailableSkus });
        }

        var lines = requestedItems
            .Select(i => new NewOrderLine(productsBySku[i.Sku].Id, i.Quantity, productsBySku[i.Sku].Price))
            .ToList();

        var orderId = await orderRepository.CreateAsync(customerName, customerEmail, lines, cancellationToken);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        return TypedResults.Created($"/orders/{orderId}", ToResponse(order!));
    }

    private static async Task<IResult> GetOrderAsync(
        Guid id,
        OrderRepository orderRepository,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToResponse(order));
    }

    private static async Task<IResult> UpdateOrderStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        OrderRepository orderRepository,
        CancellationToken cancellationToken)
    {
        var errors = UpdateOrderStatusRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var status = OrderStatus.NormalizeDecision(request.Status)!;
        var rejectionReason = status == OrderStatus.Rejected ? request.RejectionReason!.Trim() : null;

        var result = await orderRepository.UpdateStatusAsync(id, status, rejectionReason, cancellationToken);

        if (result == StatusUpdateResult.NotFound)
        {
            return TypedResults.NotFound();
        }

        var order = await orderRepository.GetByIdAsync(id, cancellationToken);

        if (result == StatusUpdateResult.AlreadyDecided)
        {
            return TypedResults.Problem(
                title: "Order has already been decided.",
                detail: $"This order is already {order!.Order.Status} and cannot be changed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["currentStatus"] = order.Order.Status });
        }

        return TypedResults.Ok(ToResponse(order!));
    }

    private static OrderResponse ToResponse(OrderDetails details) =>
        new(
            details.Order.Id,
            details.Order.Status,
            new CustomerResponse(details.Order.CustomerName, details.Order.CustomerEmail),
            details.Items
                .Select(i => new OrderItemResponse(i.Sku, i.Name, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice))
                .ToList(),
            details.Order.TotalPrice,
            DateTime.SpecifyKind(details.Order.CreatedAtUtc, DateTimeKind.Utc),
            details.Order.RejectionReason,
            details.Order.DecidedAtUtc is { } decidedAt
                ? DateTime.SpecifyKind(decidedAt, DateTimeKind.Utc)
                : null);
}