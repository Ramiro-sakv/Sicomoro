using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sicomoro.Application.Interfaces;
using Sicomoro.Domain.Interfaces;
using Sicomoro.Infrastructure.ExternalServices;
using Sicomoro.Infrastructure.Pdf;
using Sicomoro.Infrastructure.Persistence;
using Sicomoro.Infrastructure.Repositories;

namespace Sicomoro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolvePostgresConnectionString(configuration);

        services.AddDbContext<SicomoroDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<EmailSenderAdapter>();
        services.AddScoped<IEmailSender>(sp => new RetryEmailSenderDecorator(sp.GetRequiredService<EmailSenderAdapter>()));
        services.AddScoped<IWhatsAppSender, WhatsAppSenderAdapter>();
        services.AddScoped<PdfComprobanteProvider>();
        services.AddScoped<FacturacionElectronicaProvider>();
        services.AddScoped<IDocumentoFactory, DocumentoFactory>();
        return services;
    }

    private static string ResolvePostgresConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ConvertPostgresUrl(databaseUrl);
        }

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuro ConnectionStrings:DefaultConnection ni DATABASE_URL.");
    }

    private static string ConvertPostgresUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
            Password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
            SslMode = SslMode.Prefer
        };

        return builder.ConnectionString;
    }
}
