using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Services;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = "Administrador,Gerente")]
[ApiController]
[Route("api/reportes")]
public sealed class ReportesController(IReportesProxy reportes) : ControllerBase
{
    [HttpGet("ventas")]
    public async Task<ActionResult<ApiResponse<object>>> Ventas([FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await reportes.VentasAsync(desde, hasta, ct)));

    [HttpGet("inventario-bajo")]
    public async Task<ActionResult<ApiResponse<object>>> InventarioBajo(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await reportes.InventarioBajoAsync(ct)));

    [HttpGet("clientes-deudores")]
    public async Task<ActionResult<ApiResponse<object>>> ClientesDeudores(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await reportes.ClientesDeudoresAsync(ct)));

    [HttpGet("caja")]
    public async Task<ActionResult<ApiResponse<object>>> Caja([FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await reportes.CajaAsync(desde, hasta, ct)));
}
