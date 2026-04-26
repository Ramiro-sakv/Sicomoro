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
[Route("api/productos")]
public sealed class ProductosController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarProductosQuery(), ct)));

    [HttpPost]
    [Authorize(Roles = AppRoles.InventarioGestion)]
    public async Task<ActionResult<ApiResponse<object>>> Post(CrearProductoCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.InventarioGestion)]
    public async Task<ActionResult<ApiResponse<object>>> Put(Guid id, ActualizarProductoRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ActualizarProductoCommand(id, request.NombreComercial, request.TipoMadera, request.UnidadMedida, request.Largo, request.Ancho, request.Espesor, request.Calidad, request.PrecioCompra, request.PrecioVentaSugerido, request.StockMinimo, request.Estado, request.Observaciones), ct)));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.InventarioGestion)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EliminarProductoCommand(id), ct)));
}

public sealed record ActualizarProductoRequest(string NombreComercial, string TipoMadera, Domain.Enums.UnidadMedida UnidadMedida, decimal Largo, decimal Ancho, decimal Espesor, string? Calidad, decimal PrecioCompra, decimal PrecioVentaSugerido, decimal StockMinimo, Domain.Enums.EstadoRegistro Estado, string? Observaciones);
