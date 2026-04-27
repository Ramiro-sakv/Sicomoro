using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Commands;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = AppRoles.Ventas)]
[ApiController]
[Route("api/documentos")]
public sealed class DocumentosController(IMediator mediator) : ControllerBase
{
    [HttpPost("venta/{ventaId:guid}/generar")]
    public async Task<ActionResult<ApiResponse<object>>> Generar(Guid ventaId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new GenerarDocumentoVentaCommand(ventaId), ct)));

    [HttpPost("venta/{ventaId:guid}/enviar")]
    public async Task<ActionResult<ApiResponse<object>>> Enviar(Guid ventaId, EnviarDocumentoRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EnviarDocumentoVentaCommand(ventaId, request.Destino), ct)));

    [HttpGet("venta/{ventaId:guid}/descargar")]
    public async Task<IActionResult> Descargar(Guid ventaId, CancellationToken ct)
    {
        var documento = await mediator.Send(new GenerarDocumentoVentaCommand(ventaId), ct);
        if (!System.IO.File.Exists(documento.RutaArchivo))
            return NotFound(ApiResponse<object>.Fail("No se encontro el archivo PDF generado."));

        var bytes = await System.IO.File.ReadAllBytesAsync(documento.RutaArchivo, ct);
        return File(bytes, "application/pdf", $"{documento.Numero}.pdf");
    }
}

public sealed record EnviarDocumentoRequest(string Destino);
