using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicomoro.Api.DTOs;
using Sicomoro.Api.Security;
using Sicomoro.Application.Queries;

namespace Sicomoro.Api.Controllers;

[Authorize(Roles = AppRoles.Gestion)]
[ApiController]
[Route("api/auditoria")]
public sealed class AuditoriaController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] int take = 100, CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await mediator.Send(new ListarAuditoriaQuery(take), ct)));
}
