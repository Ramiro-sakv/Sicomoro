using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Commands;

namespace Sicomoro.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/sistema")]
public sealed class SistemaController(IMediator mediator) : ControllerBase
{
    [HttpPost("reiniciar-datos")]
    public async Task<ActionResult<ApiResponse<object>>> ReiniciarDatos([FromHeader(Name = "X-Sicomoro-Operation-Key")] string? claveOperacion, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ReiniciarDatosSistemaCommand(claveOperacion), ct)));
}
