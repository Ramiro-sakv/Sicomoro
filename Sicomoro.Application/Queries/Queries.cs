using MediatR;
using Sicomoro.Application.Commands;
using Sicomoro.Application.DTOs;
using Sicomoro.Domain.Enums;
using Sicomoro.Domain.Interfaces;

namespace Sicomoro.Application.Queries;

public sealed record ListarClientesQuery(string? Buscar) : IRequest<List<ClienteDto>>;
public sealed record ObtenerClienteQuery(Guid Id) : IRequest<ClienteDto>;
public sealed record ListarProveedoresQuery : IRequest<List<ProveedorDto>>;
public sealed record ListarProductosQuery : IRequest<List<ProductoDto>>;
public sealed record ListarInventarioQuery : IRequest<List<InventarioDto>>;
public sealed record ListarMovimientosInventarioQuery : IRequest<List<MovimientoInventarioDto>>;
public sealed record ListarTransportesQuery : IRequest<List<TransporteDto>>;
public sealed record ListarComprasQuery : IRequest<List<CompraDto>>;
public sealed record ListarVentasQuery : IRequest<List<VentaDto>>;
public sealed record ObtenerVentaQuery(Guid Id) : IRequest<VentaDto>;
public sealed record ListarDeudasQuery : IRequest<List<CobroDto>>;
public sealed record ListarCobrosClienteQuery(Guid ClienteId) : IRequest<List<CobroDto>>;
public sealed record ListarCajaMovimientosQuery(DateTime Desde, DateTime Hasta) : IRequest<List<CajaMovimientoDto>>;
public sealed record ListarNotificacionesQuery(bool SoloNoLeidas = false) : IRequest<List<NotificacionDto>>;
public sealed record ListarAuditoriaQuery(int Take = 100) : IRequest<List<AuditoriaDto>>;
public sealed record ReporteVentasQuery(DateTime Desde, DateTime Hasta) : IRequest<ReporteVentasDto>;
public sealed record ReporteInventarioBajoQuery : IRequest<List<InventarioDto>>;
public sealed record ReporteClientesDeudoresQuery : IRequest<List<ClienteDto>>;
public sealed record ReporteCajaQuery(DateTime Desde, DateTime Hasta) : IRequest<ReporteCajaDto>;

public sealed class QueryHandlers(IUnitOfWork uow) :
    IRequestHandler<ListarClientesQuery, List<ClienteDto>>,
    IRequestHandler<ObtenerClienteQuery, ClienteDto>,
    IRequestHandler<ListarProveedoresQuery, List<ProveedorDto>>,
    IRequestHandler<ListarProductosQuery, List<ProductoDto>>,
    IRequestHandler<ListarInventarioQuery, List<InventarioDto>>,
    IRequestHandler<ListarMovimientosInventarioQuery, List<MovimientoInventarioDto>>,
    IRequestHandler<ListarTransportesQuery, List<TransporteDto>>,
    IRequestHandler<ListarComprasQuery, List<CompraDto>>,
    IRequestHandler<ListarVentasQuery, List<VentaDto>>,
    IRequestHandler<ObtenerVentaQuery, VentaDto>,
    IRequestHandler<ListarDeudasQuery, List<CobroDto>>,
    IRequestHandler<ListarCobrosClienteQuery, List<CobroDto>>,
    IRequestHandler<ListarCajaMovimientosQuery, List<CajaMovimientoDto>>,
    IRequestHandler<ListarNotificacionesQuery, List<NotificacionDto>>,
    IRequestHandler<ListarAuditoriaQuery, List<AuditoriaDto>>,
    IRequestHandler<ReporteVentasQuery, ReporteVentasDto>,
    IRequestHandler<ReporteInventarioBajoQuery, List<InventarioDto>>,
    IRequestHandler<ReporteClientesDeudoresQuery, List<ClienteDto>>,
    IRequestHandler<ReporteCajaQuery, ReporteCajaDto>
{
    public async Task<List<ClienteDto>> Handle(ListarClientesQuery r, CancellationToken ct)
    {
        var clientes = await uow.Clientes.BuscarAsync(r.Buscar, ct);
        var result = new List<ClienteDto>();
        foreach (var cliente in clientes)
            result.Add(cliente.ToDto(await uow.Clientes.ObtenerDeudaTotalAsync(cliente.Id, ct)));
        return result;
    }

    public async Task<ClienteDto> Handle(ObtenerClienteQuery r, CancellationToken ct)
    {
        var cliente = await uow.Clientes.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        return cliente.ToDto(await uow.Clientes.ObtenerDeudaTotalAsync(cliente.Id, ct));
    }

    public async Task<List<ProveedorDto>> Handle(ListarProveedoresQuery r, CancellationToken ct) => (await uow.Proveedores.ListarAsync(ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<ProductoDto>> Handle(ListarProductosQuery r, CancellationToken ct) => (await uow.Productos.ListarAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<List<InventarioDto>> Handle(ListarInventarioQuery r, CancellationToken ct)
    {
        var inventario = await uow.Inventario.ListarAsync(ct);
        var productos = await uow.Productos.ListarAsync(ct);
        return inventario.Select(x => x.ToDto(productos.First(p => p.Id == x.ProductoMaderaId))).ToList();
    }

    public async Task<List<MovimientoInventarioDto>> Handle(ListarMovimientosInventarioQuery r, CancellationToken ct) =>
        (await uow.Inventario.ListarMovimientosAsync(ct)).Select(x => new MovimientoInventarioDto(x.Id, x.Fecha, x.ProductoMaderaId, x.Tipo, x.Cantidad, x.CostoUnitario, x.Motivo)).ToList();

    public async Task<List<TransporteDto>> Handle(ListarTransportesQuery r, CancellationToken ct) =>
        (await uow.Transportes.ListarAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<List<CompraDto>> Handle(ListarComprasQuery r, CancellationToken ct) =>
        (await uow.Compras.ListarAsync(ct)).Select(x => new CompraDto(x.Id, x.ProveedorId, x.Origen, x.Estado, x.FechaCompra, x.Detalles.Sum(d => d.Cantidad * d.PrecioCompra), x.CostoTransporte, x.OtrosCostos)).ToList();

    public async Task<List<VentaDto>> Handle(ListarVentasQuery r, CancellationToken ct) => (await uow.Ventas.ListarAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<VentaDto> Handle(ObtenerVentaQuery r, CancellationToken ct)
    {
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.Id, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        return venta.ToDto();
    }

    public async Task<List<CobroDto>> Handle(ListarDeudasQuery r, CancellationToken ct) => (await uow.Cobros.ObtenerDeudasAsync(ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<CobroDto>> Handle(ListarCobrosClienteQuery r, CancellationToken ct) => (await uow.Cobros.ObtenerPorClienteAsync(r.ClienteId, ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<CajaMovimientoDto>> Handle(ListarCajaMovimientosQuery r, CancellationToken ct) => (await uow.Caja.ListarPorRangoAsync(r.Desde, r.Hasta, ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<NotificacionDto>> Handle(ListarNotificacionesQuery r, CancellationToken ct) => (r.SoloNoLeidas ? await uow.Notificaciones.ListarNoLeidasAsync(ct) : await uow.Notificaciones.ListarAsync(ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<AuditoriaDto>> Handle(ListarAuditoriaQuery r, CancellationToken ct) => (await uow.Auditoria.ListarRecienteAsync(Math.Clamp(r.Take, 1, 500), ct)).Select(x => x.ToDto()).ToList();

    public async Task<ReporteVentasDto> Handle(ReporteVentasQuery r, CancellationToken ct)
    {
        var ventas = (await uow.Ventas.ListarAsync(ct)).Where(x => x.Fecha >= r.Desde && x.Fecha <= r.Hasta && x.Estado != EstadoVenta.Anulada).ToList();
        return new ReporteVentasDto(r.Desde, r.Hasta, ventas.Count, ventas.Sum(x => x.Total), ventas.Sum(x => x.MontoPagado), ventas.Sum(x => x.SaldoPendiente));
    }

    public async Task<List<InventarioDto>> Handle(ReporteInventarioBajoQuery r, CancellationToken ct)
    {
        var bajo = await uow.Inventario.ObtenerBajoStockAsync(ct);
        var productos = await uow.Productos.ListarAsync(ct);
        return bajo.Select(x => x.ToDto(productos.First(p => p.Id == x.ProductoMaderaId))).ToList();
    }

    public async Task<List<ClienteDto>> Handle(ReporteClientesDeudoresQuery r, CancellationToken ct)
    {
        var clientes = await uow.Clientes.ListarAsync(ct);
        var result = new List<ClienteDto>();
        foreach (var cliente in clientes)
        {
            var deuda = await uow.Clientes.ObtenerDeudaTotalAsync(cliente.Id, ct);
            if (deuda > 0) result.Add(cliente.ToDto(deuda));
        }
        return result.OrderByDescending(x => x.DeudaTotal).ToList();
    }

    public async Task<ReporteCajaDto> Handle(ReporteCajaQuery r, CancellationToken ct)
    {
        var movimientos = await uow.Caja.ListarPorRangoAsync(r.Desde, r.Hasta, ct);
        var ingresos = movimientos.Where(x => x.Tipo == TipoCajaMovimiento.Ingreso).Sum(x => x.Monto);
        var egresos = movimientos.Where(x => x.Tipo == TipoCajaMovimiento.Egreso).Sum(x => x.Monto);
        return new ReporteCajaDto(r.Desde, r.Hasta, ingresos, egresos, ingresos - egresos);
    }
}
