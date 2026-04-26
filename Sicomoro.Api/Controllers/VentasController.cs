using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/ventas")]
public sealed class VentasController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarVentasQuery(), ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ObtenerVentaQuery(id), ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Post(CrearVentaCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(new { id = await mediator.Send(command, ct) }));

    [HttpPut("{id:guid}/confirmar")]
    public async Task<ActionResult<ApiResponse<object>>> Confirmar(Guid id, ConfirmarVentaRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ConfirmarVentaCommand(id, request.MontoPagado), ct)));

    [HttpPut("{id:guid}/anular")]
    public async Task<ActionResult<ApiResponse<object>>> Anular(Guid id, AnularVentaRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new AnularVentaCommand(id, request.Motivo), ct)));
}

public sealed record ConfirmarVentaRequest(decimal MontoPagado);
public sealed record AnularVentaRequest(string Motivo);

