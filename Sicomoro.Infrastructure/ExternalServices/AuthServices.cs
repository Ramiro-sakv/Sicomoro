using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sicomoro.Application.Interfaces;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;
using Sicomoro.Domain.Interfaces;

namespace Sicomoro.Infrastructure.ExternalServices;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return HashWithSalt(password, salt);
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split(':');
        if (parts.Length != 3 || parts[0] != "pbkdf2") return false;
        var salt = Convert.FromBase64String(parts[1]);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(HashWithSalt(password, salt)), Encoding.UTF8.GetBytes(hash));
    }

    public static string HashDeterministic(string password) => HashWithSalt(password, Encoding.UTF8.GetBytes("sicomoro-seed-001"));

    private static string HashWithSalt(string password, byte[] salt)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CrearToken(Usuario usuario)
    {
        var key = configuration["Jwt:Key"] ?? "Sicomoro-dev-key-change-this-value-32chars";
        var issuer = configuration["Jwt:Issuer"] ?? "Sicomoro";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
            new Claim("rol", usuario.Rol.ToString())
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, issuer, claims, expires: DateTime.UtcNow.AddHours(10), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class AuthService(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var usuario = await uow.Usuarios.ObtenerPorEmailAsync(email, cancellationToken) ?? throw new UnauthorizedAccessException("Credenciales invalidas.");
        if (!hasher.Verify(password, usuario.PasswordHash)) throw new UnauthorizedAccessException("Credenciales invalidas.");
        return new AuthResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, jwt.CrearToken(usuario));
    }

    public async Task<AuthResponse> RegisterAsync(string nombre, string email, string password, RolSistema rol, CancellationToken cancellationToken = default)
    {
        if (await uow.Usuarios.ObtenerPorEmailAsync(email, cancellationToken) is not null)
            throw new InvalidOperationException("El email ya esta registrado.");
        var usuario = new Usuario(nombre, email, hasher.Hash(password), rol);
        await uow.Usuarios.AgregarAsync(usuario, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return new AuthResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, jwt.CrearToken(usuario));
    }
}

