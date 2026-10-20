using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Orders.Commands;
using OrderService.Application.Orders.Queries;
using OrderService.Domain.Orders;
using CreateOrderResponse = OrderService.Application.Common.Models.CreateOrderResponse;

namespace OrderService.Api.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class OrdersController(ISender sender) : BaseController
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.AccountId,
            request.SymbolId,
            request.Side,
            request.OrderType,
            request.Quantity,
            request.Price,
            request.TimeInForce);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetOrdersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] string? exchange = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOrdersQuery(status, exchange, pageNumber, pageSize);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }
    
    [HttpPost("{internalId}/cancel")]
    [ProducesResponseType(typeof(CancelOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(string internalId, CancellationToken cancellationToken = default)
    {
        var command = new CancelOrderCommand(internalId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }
}

public record CreateOrderRequest(
    int AccountId,
    string SymbolId,
    OrderSide Side,
    OrderType OrderType,
    decimal Quantity,
    decimal? Price = null,
    TimeInForce? TimeInForce = null
);
