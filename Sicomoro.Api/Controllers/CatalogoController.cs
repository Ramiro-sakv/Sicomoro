using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public sealed class CatalogoController(IMediator mediator) : ControllerBase
{
    [HttpGet("publico")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Publico(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarAnunciosCatalogoQuery(true), ct)));

    [HttpGet("anuncios")]
    [Authorize(Roles = AppRoles.Gestion)]
    public async Task<ActionResult<ApiResponse<object>>> GetAnuncios(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarAnunciosCatalogoQuery(false), ct)));

    [HttpPost("anuncios")]
    [Authorize(Roles = AppRoles.Gestion)]
    public async Task<ActionResult<ApiResponse<object>>> Crear(CrearAnuncioCatalogoCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpPut("anuncios/{id:guid}")]
    [Authorize(Roles = AppRoles.Gestion)]
    public async Task<ActionResult<ApiResponse<object>>> Actualizar(Guid id, ActualizarAnuncioCatalogoRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ActualizarAnuncioCatalogoCommand(id, request.ProductoId, request.Titulo, request.Subtitulo, request.Descripcion, request.ImagenUrl, request.PrecioTexto, request.Etiqueta, request.CtaTexto, request.CtaUrl, request.Orden, request.Publicado), ct)));

    [HttpDelete("anuncios/{id:guid}")]
    [Authorize(Roles = AppRoles.Gestion)]
    public async Task<ActionResult<ApiResponse<object>>> Eliminar(Guid id, [FromHeader(Name = "X-Sicomoro-Operation-Key")] string? claveOperacion, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EliminarAnuncioCatalogoCommand(id, claveOperacion), ct)));
}

public sealed record ActualizarAnuncioCatalogoRequest(Guid? ProductoId, string Titulo, string? Subtitulo, string Descripcion, string? ImagenUrl, string? PrecioTexto, string? Etiqueta, string? CtaTexto, string? CtaUrl, int Orden, bool Publicado);
