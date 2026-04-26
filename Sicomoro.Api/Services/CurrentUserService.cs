using System.Security.Claims;
using Sicomoro.Application.Interfaces;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public RolSistema? Rol
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? accessor.HttpContext?.User.FindFirstValue("rol");
            return Enum.TryParse<RolSistema>(value, out var rol) ? rol : null;
        }
    }
}

