using OrderIntake.Api.Dtos;
using OrderIntake.Api.Repositories;

namespace OrderIntake.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products", GetProductsAsync)
            .WithName("GetProducts")
            .WithTags("Products")
            .Produces<List<ProductResponse>>();

        return app;
    }

    private static async Task<IResult> GetProductsAsync(
        ProductRepository productRepository,
        CancellationToken cancellationToken)
    {
        var products = await productRepository.GetActiveAsync(cancellationToken);

        return TypedResults.Ok(products
            .Select(p => new ProductResponse(p.Sku, p.Name, p.Price))
            .ToList());
    }
}