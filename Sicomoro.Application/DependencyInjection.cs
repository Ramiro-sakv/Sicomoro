using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sicomoro.Application.Validators;
using Sicomoro.Domain.DomainServices;

namespace Sicomoro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IPricingStrategy, PrecioNormalStrategy>();
        services.AddScoped<IPricingStrategy, PrecioMayoristaStrategy>();
        services.AddScoped<IPricingStrategy, PrecioClienteFrecuenteStrategy>();
        services.AddScoped<IPricingStrategy, PrecioConDescuentoManualStrategy>();
        services.AddScoped<PricingService>();
        services.AddScoped<EstadoVentaFactory>();

        services.AddScoped<ValidarClienteActivoHandler>();
        services.AddScoped<ValidarStockDisponibleHandler>();
        services.AddScoped<ValidarPrecioValidoHandler>();
        services.AddScoped<ValidarCreditoClienteHandler>();
        services.AddScoped<ValidarPermisosUsuarioHandler>();
        services.AddScoped<VentaValidationChain>();

        return services;
    }
}

