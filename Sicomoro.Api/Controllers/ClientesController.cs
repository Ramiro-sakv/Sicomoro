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
[Route("api/clientes")]
public sealed class ClientesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] string? buscar, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarClientesQuery(buscar), ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ObtenerClienteQuery(id), ct)));

    [HttpPost]
    [Authorize(Roles = AppRoles.Ventas)]
    public async Task<ActionResult<ApiResponse<object>>> Post(CrearClienteCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Ventas)]
    public async Task<ActionResult<ApiResponse<object>>> Put(Guid id, ActualizarClienteRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ActualizarClienteCommand(id, request.NombreRazonSocial, request.CiNit, request.Telefono, request.Direccion, request.Ciudad, request.Notas, request.Estado), ct)));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Gestion)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, [FromHeader(Name = "X-Sicomoro-Operation-Key")] string? claveOperacion, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EliminarClienteCommand(id, claveOperacion), ct)));
}

public sealed record ActualizarClienteRequest(string NombreRazonSocial, string? CiNit, string? Telefono, string? Direccion, string? Ciudad, string? Notas, Domain.Enums.EstadoRegistro Estado);
