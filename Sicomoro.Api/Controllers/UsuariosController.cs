using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Application.Commands;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarUsuariosQuery(), ct)));

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> Delete([FromQuery] string email, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new EliminarUsuarioPorEmailCommand(email), ct)));
}
