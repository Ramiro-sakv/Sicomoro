using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = AppRoles.Cobranza)]
[ApiController]
[Route("api/caja")]
public sealed class CajaController(IMediator mediator) : ControllerBase
{
    [HttpGet("movimientos")]
    public async Task<ActionResult<ApiResponse<object>>> Movimientos([FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarCajaMovimientosQuery(desde, hasta), ct)));

    [HttpGet("cierre")]
    public async Task<ActionResult<ApiResponse<object>>> Cierre([FromQuery] DateTime fecha, [FromQuery] decimal saldoApertura = 0, CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ObtenerCajaCierreQuery(fecha, saldoApertura), ct)));

    [HttpPost("movimientos")]
    public async Task<ActionResult<ApiResponse<object>>> Registrar(RegistrarCajaMovimientoCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpPost("cierre")]
    public async Task<ActionResult<ApiResponse<object>>> RegistrarCierre(RegistrarCajaCierreCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));
}
