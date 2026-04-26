using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = AppRoles.Staff)]
[ApiController]
[Route("api/inventario")]
public sealed class InventarioController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarInventarioQuery(), ct)));

    [HttpPost("ajuste")]
    [Authorize(Roles = AppRoles.InventarioGestion)]
    public async Task<ActionResult<ApiResponse<object>>> Ajuste(AjustarInventarioCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpGet("movimientos")]
    public async Task<ActionResult<ApiResponse<object>>> Movimientos(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarMovimientosInventarioQuery(), ct)));
}
