using Microsoft.AspNetCore.Http.HttpResults;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using OrderService.Domain.Enums;

namespace OrderService.API.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/orders")
            .WithTags("Orders");

        group.MapGet("/", GetAll)
            .WithSummary("Gets all orders.");

        group.MapGet("{id:guid}", GetById)
            .WithSummary("Gets a single order by its ID.");

        group.MapGet("customer/{customerId}", GetByCustomer)
            .WithSummary("Gets all orders for a specific customer.");

        group.MapPost("/", Create)
            .WithSummary("Creates a new order.");

        group.MapPatch("{id:guid}/status", UpdateStatus)
            .WithSummary("Updates the status of an order. Enforces the valid state machine transitions.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<OrderDto>>> GetAll(
        IOrderService orders,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await orders.GetAllAsync(cancellationToken));
    }

    private static async Task<Results<Ok<OrderDto>, NotFound>> GetById(
        Guid id,
        IOrderService orders,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
    }

    private static async Task<Ok<IReadOnlyList<OrderDto>>> GetByCustomer(
        string customerId,
        IOrderService orders,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await orders.GetByCustomerAsync(customerId, cancellationToken));
    }

    private static async Task<Results<Created<Guid>, BadRequest<string>>> Create(
        CreateOrderCommand command,
        IOrderService orders,
        CancellationToken cancellationToken)
    {
        var result = await orders.CreateAsync(command, cancellationToken);
        if (!result.IsSuccess) return TypedResults.BadRequest(result.Error!);
        return TypedResults.Created($"api/orders/{result.Value}", result.Value);
    }

    private static async Task<Results<NoContent, BadRequest<string>, NotFound<string>>> UpdateStatus(
        Guid id,
        UpdateStatusRequest request,
        IOrderService orders,
        CancellationToken cancellationToken)
    {
        var result = await orders.UpdateStatusAsync(new UpdateOrderStatusCommand(id, request.Status), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.Contains("not found")
                ? TypedResults.NotFound(result.Error!)
                : TypedResults.BadRequest(result.Error!);
        }
        return TypedResults.NoContent();
    }
}

public record UpdateStatusRequest(OrderStatus Status);
