using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Venturacom.Application.Customers.Commands;
using Venturacom.Application.Customers.Queries;

namespace Venturacom.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await mediator.Send(new ListCustomersQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken) => Ok(await mediator.Send(command, cancellationToken));
}
