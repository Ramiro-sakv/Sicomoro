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
[Route("api/compras")]
public sealed class ComprasController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarComprasQuery(), ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Post(CrearCompraCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(new { id = await mediator.Send(command, ct) }));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Put(Guid id, ActualizarCompraRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ActualizarCompraCommand(id, request.ProveedorId, request.Origen, request.FechaCompra, request.FechaEstimadaLlegada, request.CostoTransporte, request.OtrosCostos, request.Observaciones, request.Detalles), ct)));

    [HttpPut("{id:guid}/recibir")]
    public async Task<ActionResult<ApiResponse<object>>> Recibir(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new RecibirCompraCommand(id), ct)));

    [HttpPut("{id:guid}/recalcular-costo")]
    public async Task<ActionResult<ApiResponse<object>>> RecalcularCosto(Guid id, RecalcularCostoCompraRequest request, [FromHeader(Name = "X-Sicomoro-Operation-Key")] string? claveOperacion, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new RecalcularCostoCompraCommand(id, request.PrecioCompraUnitario, claveOperacion), ct)));
}

public sealed record ActualizarCompraRequest(Guid ProveedorId, string Origen, DateTime FechaCompra, DateTime? FechaEstimadaLlegada, decimal CostoTransporte, decimal OtrosCostos, string? Observaciones, IReadOnlyCollection<Sicomoro.Application.DTOs.CompraDetalleInput> Detalles);
public sealed record RecalcularCostoCompraRequest(decimal PrecioCompraUnitario);
