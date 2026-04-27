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
[Route("api/notificaciones")]
public sealed class NotificacionesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] bool soloNoLeidas, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarNotificacionesQuery(soloNoLeidas), ct)));

    [HttpPost("whatsapp-prueba")]
    [Authorize(Roles = AppRoles.Gestion)]
    public async Task<ActionResult<ApiResponse<object>>> EnviarWhatsAppPrueba(WhatsAppPruebaRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EnviarWhatsAppPruebaCommand(request.Mensaje), ct)));
}

public sealed record WhatsAppPruebaRequest(string Mensaje);
