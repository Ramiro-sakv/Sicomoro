using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/cobros")]
public sealed class CobrosController(IMediator mediator) : ControllerBase
{
    [HttpPost("pagos")]
    public async Task<ActionResult<ApiResponse<object>>> Pago(RegistrarPagoCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpGet("deudas")]
    public async Task<ActionResult<ApiResponse<object>>> Deudas(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarDeudasQuery(), ct)));

    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Cliente(Guid clienteId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarCobrosClienteQuery(clienteId), ct)));
}

