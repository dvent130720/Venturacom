using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Venturacom.Application.Products.Commands;
using Venturacom.Application.Products.Queries;

namespace Venturacom.API.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await mediator.Send(new ListProductsQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken) => Ok(await mediator.Send(command, cancellationToken));
}
