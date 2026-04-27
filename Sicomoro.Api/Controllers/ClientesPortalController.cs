using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Application.Commands;

namespace Sicomoro.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/clientes-portal")]
public sealed class ClientesPortalController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login(LoginClientePortalCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register(RegistrarClientePortalCommand command, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(command, ct)));
}
