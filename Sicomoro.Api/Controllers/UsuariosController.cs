using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<object>>> Me(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ObtenerMiPerfilQuery(), ct)));

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<object>>> ActualizarMiPerfil(ActualizarMiPerfilCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpPut("me/password")]
    public async Task<ActionResult<ApiResponse<object>>> CambiarMiPassword(CambiarMiPasswordCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarUsuariosQuery(), ct)));

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Post(CrearUsuarioCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EliminarUsuarioCommand(id), ct)));

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetearPassword(Guid id, ResetearUsuarioPasswordRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ResetearUsuarioPasswordCommand(id, request.NuevaPassword), ct)));

    [Authorize(Roles = "Administrador")]
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> Delete([FromQuery] string email, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EliminarUsuarioPorEmailCommand(email), ct)));
}

public sealed record ResetearUsuarioPasswordRequest(string NuevaPassword);
