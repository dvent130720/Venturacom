using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Venturacom.Application.Invoices.Commands;
using Venturacom.Application.Invoices.Queries;

namespace Venturacom.API.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await mediator.Send(new ListInvoicesQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceCommand command, CancellationToken cancellationToken) => Ok(await mediator.Send(command, cancellationToken));
}
