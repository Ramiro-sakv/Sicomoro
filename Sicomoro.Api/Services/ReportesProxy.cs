using MediatR;
using Sicomoro.Application.DTOs;
using Sicomoro.Application.Interfaces;
using Sicomoro.Application.Queries;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Api.Services;

public interface IReportesProxy
{
    Task<ReporteVentasDto> VentasAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<List<InventarioDto>> InventarioBajoAsync(CancellationToken ct);
    Task<List<ClienteDto>> ClientesDeudoresAsync(CancellationToken ct);
    Task<ReporteCajaDto> CajaAsync(DateTime desde, DateTime hasta, CancellationToken ct);
}

public sealed class ReportesProxy(IMediator mediator, ICurrentUserService currentUser) : IReportesProxy
{
    public Task<ReporteVentasDto> VentasAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        VerificarPermiso();
        return mediator.Send(new ReporteVentasQuery(desde, hasta), ct);
    }

    public Task<List<InventarioDto>> InventarioBajoAsync(CancellationToken ct)
    {
        VerificarPermiso();
        return mediator.Send(new ReporteInventarioBajoQuery(), ct);
    }

    public Task<List<ClienteDto>> ClientesDeudoresAsync(CancellationToken ct)
    {
        VerificarPermiso();
        return mediator.Send(new ReporteClientesDeudoresQuery(), ct);
    }

    public Task<ReporteCajaDto> CajaAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        VerificarPermiso();
        return mediator.Send(new ReporteCajaQuery(desde, hasta), ct);
    }

    private void VerificarPermiso()
    {
        if (currentUser.Rol is not (RolSistema.Administrador or RolSistema.Gerente))
            throw new UnauthorizedAccessException("No tiene permiso para reportes sensibles.");
    }
}

