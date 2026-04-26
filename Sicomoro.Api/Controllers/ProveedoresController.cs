using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = AppRoles.InventarioGestion)]
[ApiController]
[Route("api/proveedores")]
public sealed class ProveedoresController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarProveedoresQuery(), ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Post(CrearProveedorCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));
}
