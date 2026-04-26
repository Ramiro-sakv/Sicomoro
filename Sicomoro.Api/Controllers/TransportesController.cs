using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = AppRoles.InventarioGestion)]
[ApiController]
[Route("api/transportes")]
public sealed class TransportesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarTransportesQuery(), ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Post(CrearTransporteCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpPut("{id:guid}/estado")]
    public async Task<ActionResult<ApiResponse<object>>> Estado(Guid id, ActualizarEstadoTransporteRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ActualizarEstadoTransporteCommand(id, request.Estado, request.FechaLlegada), ct)));
}

public sealed record ActualizarEstadoTransporteRequest(EstadoTransporte Estado, DateTime? FechaLlegada);
