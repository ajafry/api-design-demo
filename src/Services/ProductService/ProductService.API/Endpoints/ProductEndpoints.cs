using Microsoft.AspNetCore.Http.HttpResults;
using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Application.Services;

namespace ProductService.API.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/products")
            .WithTags("Products");

        group.MapGet("/", GetAll)
            .WithSummary("Gets all active products.");

        group.MapGet("{id:guid}", GetById)
            .WithSummary("Gets a single product by its ID.");

        group.MapGet("category/{category}", GetByCategory)
            .WithSummary("Gets products filtered by category.");

        group.MapPost("/", Create)
            .WithSummary("Creates a new product.");

        group.MapPut("{id:guid}", Update)
            .WithSummary("Updates an existing product.");

        group.MapDelete("{id:guid}", Delete)
            .WithSummary("Deletes (deactivates) a product.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductDto>>> GetAll(
        IProductService products,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await products.GetAllAsync(cancellationToken));
    }

    private static async Task<Results<Ok<ProductDto>, NotFound>> GetById(
        Guid id,
        IProductService products,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<Ok<IReadOnlyList<ProductDto>>> GetByCategory(
        string category,
        IProductService products,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await products.GetByCategoryAsync(category, cancellationToken));
    }

    private static async Task<Results<Created<Guid>, BadRequest<string>>> Create(
        CreateProductCommand command,
        IProductService products,
        CancellationToken cancellationToken)
    {
        var result = await products.CreateAsync(command, cancellationToken);
        if (!result.IsSuccess) return TypedResults.BadRequest(result.Error!);
        return TypedResults.Created($"api/products/{result.Value}", result.Value);
    }

    private static async Task<Results<NoContent, NotFound<string>>> Update(
        Guid id,
        UpdateProductRequest request,
        IProductService products,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Currency, request.StockQuantity, request.Category);
        var result = await products.UpdateAsync(command, cancellationToken);
        if (!result.IsSuccess) return TypedResults.NotFound(result.Error!);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<string>>> Delete(
        Guid id,
        IProductService products,
        CancellationToken cancellationToken)
    {
        var result = await products.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess) return TypedResults.NotFound(result.Error!);
        return TypedResults.NoContent();
    }
}

public record UpdateProductRequest(string Name, string Description, decimal Price, string Currency, int StockQuantity, string Category);
