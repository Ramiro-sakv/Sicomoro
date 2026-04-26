using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notificaciones")]
public sealed class NotificacionesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] bool soloNoLeidas, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarNotificacionesQuery(soloNoLeidas), ct)));
}

