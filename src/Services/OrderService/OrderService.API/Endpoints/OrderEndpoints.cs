using Microsoft.AspNetCore.Http.HttpResults;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using OrderService.Domain.Enums;

namespace OrderService.API.Endpoints;

public static partial class OrderEndpoints
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
        ILogger<OrderEndpointsLog> logger,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            LogOrderNotFound(logger, id);
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(order);
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
        ILogger<OrderEndpointsLog> logger,
        CancellationToken cancellationToken)
    {
        var result = await orders.CreateAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            LogOrderCreateFailed(logger, result.Error!);
            return TypedResults.BadRequest(result.Error!);
        }
        return TypedResults.Created($"api/orders/{result.Value}", result.Value);
    }

    private static async Task<Results<NoContent, BadRequest<string>, NotFound<string>>> UpdateStatus(
        Guid id,
        UpdateStatusRequest request,
        IOrderService orders,
        ILogger<OrderEndpointsLog> logger,
        CancellationToken cancellationToken)
    {
        var result = await orders.UpdateStatusAsync(new UpdateOrderStatusCommand(id, request.Status), cancellationToken);
        if (!result.IsSuccess)
        {
            LogOrderStatusUpdateFailed(logger, id, request.Status, result.Error!);
            return result.Error!.Contains("not found")
                ? TypedResults.NotFound(result.Error!)
                : TypedResults.BadRequest(result.Error!);
        }
        return TypedResults.NoContent();
    }

    // ----- Structured log definitions (source-generated, zero-allocation) -----

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Order {OrderId} not found")]
    private static partial void LogOrderNotFound(ILogger logger, Guid orderId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Failed to create order: {Error}")]
    private static partial void LogOrderCreateFailed(ILogger logger, string error);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Failed to update order {OrderId} to status {Status}: {Error}")]
    private static partial void LogOrderStatusUpdateFailed(ILogger logger, Guid orderId, OrderStatus status, string error);
}

public record UpdateStatusRequest(OrderStatus Status);

/// <summary>
/// Marker type used as the <see cref="ILogger{TCategoryName}"/> category for order endpoints.
/// A non-static type is required because <see cref="OrderEndpoints"/> is static and cannot
/// be used as a generic type argument.
/// </summary>
public sealed class OrderEndpointsLog;
