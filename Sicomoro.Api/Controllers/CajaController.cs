using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/caja")]
public sealed class CajaController(IMediator mediator) : ControllerBase
{
    [HttpGet("movimientos")]
    public async Task<ActionResult<ApiResponse<object>>> Movimientos([FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarCajaMovimientosQuery(desde, hasta), ct)));

    [HttpPost("movimientos")]
    public async Task<ActionResult<ApiResponse<object>>> Registrar(RegistrarCajaMovimientoCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));
}

