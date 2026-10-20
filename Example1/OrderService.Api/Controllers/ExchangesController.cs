using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Exchanges.Queries;

namespace OrderService.Api.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class ExchangesController(ISender sender) : BaseController
{
    [HttpGet("names")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExchangeNames(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExchangeNamesQuery(), cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("{exchange}/environments")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExchangeEnvironments(string exchange, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExchangeEnvironmentsQuery(exchange), cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetExchangesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExchanges(
        [FromQuery] string? query = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] OrderDirection? orderDirection = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetExchangesQuery(query, orderBy, orderDirection, page, pageSize), cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("{exchange}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetExchangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExchange(string exchange, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExchangeQuery(exchange), cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("{exchange}/symbols")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetExchangeSymbolsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExchangeSymbols(string exchange, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExchangeSymbolsQuery(exchange), cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("{exchange}/fees")]
    [ProducesResponseType(typeof(GetExchangeFeesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExchangeFees(
        string exchange,
        [FromQuery] string symbolId,
        [FromQuery] Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetExchangeFeesQuery(accountId, exchange, symbolId), cancellationToken);

        if (result.IsError)
            return Problem(result.Errors);

        return Ok(result.Value);
    }
}
