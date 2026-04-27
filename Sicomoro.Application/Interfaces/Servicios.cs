using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    RolSistema? Rol { get; }
}

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(string nombre, string email, string password, RolSistema rol, string? ciNit = null, string? telefono = null, string? direccion = null, string? cargo = null, string? notas = null, CancellationToken cancellationToken = default);
}

public sealed record AuthResponse(Guid UsuarioId, string Nombre, string Email, RolSistema Rol, string Token);

public interface IUserCreationKeyValidator
{
    bool IsValid(string? key);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    string CrearToken(Usuario usuario);
}

public interface IAuditoriaService
{
    Task RegistrarAsync(string accion, string entidad, Guid? entidadId, string? datosAntes, string? datosDespues, CancellationToken cancellationToken = default);
}

public interface INotificacionService
{
    Task CrearAsync(TipoNotificacion tipo, string titulo, string mensaje, Guid? usuarioId = null, CancellationToken cancellationToken = default);
}

public interface IFacturacionProvider
{
    Task<DocumentoVenta> GenerarDocumentoVentaAsync(Venta venta, Guid usuarioId, string usuarioNombre, CancellationToken cancellationToken = default);
    Task EnviarDocumentoAsync(DocumentoVenta documento, string destino, CancellationToken cancellationToken = default);
    Task AnularDocumentoAsync(DocumentoVenta documento, string motivo, CancellationToken cancellationToken = default);
}

public interface IDocumentoFactory
{
    IFacturacionProvider Crear(TipoDocumentoVenta tipo);
}

public interface IEmailSender
{
    Task EnviarAsync(string destino, string asunto, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default);
}

public interface IWhatsAppSender
{
    Task EnviarAsync(string telefono, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default);
}
