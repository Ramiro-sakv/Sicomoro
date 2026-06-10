using MediatR;
using Sicomoro.Application.Commands;
using Sicomoro.Application.DTOs;
using Sicomoro.Application.Interfaces;
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
public sealed record ObtenerCajaCierreQuery(DateTime Fecha, decimal SaldoApertura = 0) : IRequest<CajaCierreDto>;
public sealed record ListarNotificacionesQuery(bool SoloNoLeidas = false) : IRequest<List<NotificacionDto>>;
public sealed record ListarAuditoriaQuery(int Take = 100) : IRequest<List<AuditoriaDto>>;
public sealed record ListarUsuariosQuery : IRequest<List<UsuarioDto>>;
public sealed record ObtenerMiPerfilQuery : IRequest<UsuarioDto>;
public sealed record ObtenerDocumentoVentaArchivoQuery(Guid VentaId, TipoDocumentoVenta Tipo = TipoDocumentoVenta.ComprobanteVenta) : IRequest<DocumentoArchivoDto?>;
public sealed record ReporteVentasQuery(DateTime Desde, DateTime Hasta) : IRequest<ReporteVentasDto>;
public sealed record ReporteInventarioBajoQuery : IRequest<List<InventarioDto>>;
public sealed record ReporteClientesDeudoresQuery : IRequest<List<ClienteDto>>;
public sealed record ReporteCajaQuery(DateTime Desde, DateTime Hasta) : IRequest<ReporteCajaDto>;
public sealed record ReporteNegocioAvanzadoQuery(DateTime Desde, DateTime Hasta) : IRequest<ReporteNegocioDto>;
public sealed record ObtenerProductoHistorialQuery(Guid ProductoId) : IRequest<List<ProductoHistorialDto>>;
public sealed record ListarAnunciosCatalogoQuery(bool SoloPublicados) : IRequest<List<AnuncioCatalogoDto>>;

public sealed class QueryHandlers(IUnitOfWork uow, ICurrentUserService currentUser) :
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
    IRequestHandler<ObtenerCajaCierreQuery, CajaCierreDto>,
    IRequestHandler<ListarNotificacionesQuery, List<NotificacionDto>>,
    IRequestHandler<ListarAuditoriaQuery, List<AuditoriaDto>>,
    IRequestHandler<ListarUsuariosQuery, List<UsuarioDto>>,
    IRequestHandler<ObtenerMiPerfilQuery, UsuarioDto>,
    IRequestHandler<ObtenerDocumentoVentaArchivoQuery, DocumentoArchivoDto?>,
    IRequestHandler<ReporteVentasQuery, ReporteVentasDto>,
    IRequestHandler<ReporteInventarioBajoQuery, List<InventarioDto>>,
    IRequestHandler<ReporteClientesDeudoresQuery, List<ClienteDto>>,
    IRequestHandler<ReporteCajaQuery, ReporteCajaDto>,
    IRequestHandler<ReporteNegocioAvanzadoQuery, ReporteNegocioDto>,
    IRequestHandler<ObtenerProductoHistorialQuery, List<ProductoHistorialDto>>,
    IRequestHandler<ListarAnunciosCatalogoQuery, List<AnuncioCatalogoDto>>
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
        return productos
            .OrderBy(x => x.NombreComercial)
            .Select(producto =>
            {
                var stock = inventario.FirstOrDefault(x => x.ProductoMaderaId == producto.Id);
                return stock is null
                    ? new InventarioDto(Guid.Empty, producto.Id, producto.NombreComercial, 0, producto.StockMinimo, null)
                    : stock.ToDto(producto);
            })
            .ToList();
    }

    public async Task<List<MovimientoInventarioDto>> Handle(ListarMovimientosInventarioQuery r, CancellationToken ct) =>
        (await uow.Inventario.ListarMovimientosAsync(ct)).Select(x => new MovimientoInventarioDto(x.Id, x.Fecha, x.ProductoMaderaId, x.Tipo, x.Cantidad, x.CostoUnitario, x.Motivo)).ToList();

    public async Task<List<TransporteDto>> Handle(ListarTransportesQuery r, CancellationToken ct) =>
        (await uow.Transportes.ListarAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<List<CompraDto>> Handle(ListarComprasQuery r, CancellationToken ct) =>
        (await uow.Compras.ListarAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<List<VentaDto>> Handle(ListarVentasQuery r, CancellationToken ct) => (await uow.Ventas.ListarAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<VentaDto> Handle(ObtenerVentaQuery r, CancellationToken ct)
    {
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.Id, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        return venta.ToDto();
    }

    public async Task<List<CobroDto>> Handle(ListarDeudasQuery r, CancellationToken ct) => (await uow.Cobros.ObtenerDeudasAsync(ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<CobroDto>> Handle(ListarCobrosClienteQuery r, CancellationToken ct) => (await uow.Cobros.ObtenerPorClienteAsync(r.ClienteId, ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<CajaMovimientoDto>> Handle(ListarCajaMovimientosQuery r, CancellationToken ct) => (await uow.Caja.ListarPorRangoAsync(r.Desde, r.Hasta, ct)).Select(x => x.ToDto()).ToList();

    public async Task<CajaCierreDto> Handle(ObtenerCajaCierreQuery r, CancellationToken ct)
    {
        var fecha = DateTime.SpecifyKind(r.Fecha.Date, DateTimeKind.Utc);
        var cierre = await uow.CajaCierres.ObtenerPorFechaAsync(fecha, ct);
        if (cierre is not null) return cierre.ToDto();

        var (desde, hasta) = BoliviaDateRange(fecha, fecha);
        var movimientos = await uow.Caja.ListarPorRangoAsync(desde, hasta, ct);
        var ingresos = movimientos.Where(x => x.Tipo == TipoCajaMovimiento.Ingreso).Sum(x => x.Monto);
        var egresos = movimientos.Where(x => x.Tipo == TipoCajaMovimiento.Egreso).Sum(x => x.Monto);
        var esperado = r.SaldoApertura + ingresos - egresos;
        return new CajaCierreDto(Guid.Empty, fecha, r.SaldoApertura, ingresos, egresos, esperado, esperado, 0, null, currentUser.UserId ?? Guid.Empty);
    }

    public async Task<List<NotificacionDto>> Handle(ListarNotificacionesQuery r, CancellationToken ct) => (r.SoloNoLeidas ? await uow.Notificaciones.ListarNoLeidasAsync(ct) : await uow.Notificaciones.ListarAsync(ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<AuditoriaDto>> Handle(ListarAuditoriaQuery r, CancellationToken ct) => (await uow.Auditoria.ListarRecienteAsync(Math.Clamp(r.Take, 1, 500), ct)).Select(x => x.ToDto()).ToList();
    public async Task<List<UsuarioDto>> Handle(ListarUsuariosQuery r, CancellationToken ct) => (await uow.Usuarios.ListarAsync(ct)).Select(x => x.ToDto()).OrderBy(x => x.Nombre).ToList();
    public async Task<UsuarioDto> Handle(ObtenerMiPerfilQuery r, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Sesion invalida.");
        var usuario = await uow.Usuarios.ObtenerPorIdAsync(userId, ct) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        return usuario.ToDto();
    }

    public async Task<DocumentoArchivoDto?> Handle(ObtenerDocumentoVentaArchivoQuery r, CancellationToken ct)
    {
        var documento = await uow.Documentos.ObtenerUltimoPorVentaAsync(r.VentaId, r.Tipo, ct);
        return documento is null ? null : new DocumentoArchivoDto(documento.Numero, documento.RutaArchivo);
    }

    public async Task<ReporteVentasDto> Handle(ReporteVentasQuery r, CancellationToken ct)
    {
        var ventas = (await uow.Ventas.ListarAsync(ct)).Where(x => x.Fecha >= r.Desde && x.Fecha <= r.Hasta && x.Estado != EstadoVenta.Anulada && x.Estado != EstadoVenta.Pendiente).ToList();
        return new ReporteVentasDto(r.Desde, r.Hasta, ventas.Count, ventas.Sum(x => x.Total), ventas.Sum(x => x.MontoPagado), ventas.Sum(x => x.SaldoPendiente));
    }

    public async Task<List<InventarioDto>> Handle(ReporteInventarioBajoQuery r, CancellationToken ct)
    {
        var inventario = await uow.Inventario.ListarAsync(ct);
        var productos = await uow.Productos.ListarAsync(ct);
        return productos
            .Where(x => x.Estado == EstadoRegistro.Activo)
            .Select(producto =>
            {
                var stock = inventario.FirstOrDefault(x => x.ProductoMaderaId == producto.Id);
                return stock is null
                    ? new InventarioDto(Guid.Empty, producto.Id, producto.NombreComercial, 0, producto.StockMinimo, null)
                    : stock.ToDto(producto);
            })
            .Where(x => x.StockMinimo > 0 && x.StockActual <= x.StockMinimo)
            .OrderBy(x => x.StockActual - x.StockMinimo)
            .ToList();
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

    public async Task<ReporteNegocioDto> Handle(ReporteNegocioAvanzadoQuery r, CancellationToken ct)
    {
        var (desde, hasta) = BoliviaDateRange(r.Desde, r.Hasta);
        var ventas = (await uow.Ventas.ListarAsync(ct))
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta && x.Estado != EstadoVenta.Anulada && x.Estado != EstadoVenta.Pendiente)
            .ToList();
        var compras = (await uow.Compras.ListarAsync(ct))
            .Where(x => x.FechaCompra.Date >= r.Desde.Date && x.FechaCompra.Date <= r.Hasta.Date && x.Estado == EstadoCompra.Recibida)
            .ToList();
        var productos = await uow.Productos.ListarAsync(ct);
        var inventario = await uow.Inventario.ListarAsync(ct);
        var clientes = await uow.Clientes.ListarAsync(ct);

        var productosMap = productos.ToDictionary(x => x.Id);
        var clientesMap = clientes.ToDictionary(x => x.Id);
        var ventasConfirmadas = ventas.Sum(x => x.Total);
        var comprasRecibidas = compras.Sum(x => x.Detalles.Sum(d => d.Cantidad * d.PrecioCompra) + x.CostoTransporte + x.OtrosCostos);
        var utilidadBruta = ventas.Sum(venta => venta.Detalles.Sum(detalle =>
        {
            var costo = productosMap.TryGetValue(detalle.ProductoMaderaId, out var producto) ? producto.PrecioCompra : 0;
            return detalle.Subtotal - (detalle.Cantidad * costo);
        }));
        var margenPromedio = ventasConfirmadas > 0 ? utilidadBruta / ventasConfirmadas * 100 : 0;
        var inventarioValorCosto = inventario.Sum(x =>
        {
            var precio = productosMap.TryGetValue(x.ProductoMaderaId, out var producto) ? producto.PrecioCompra : 0;
            return x.StockActual * precio;
        });
        var inventarioValorVenta = inventario.Sum(x =>
        {
            var precio = productosMap.TryGetValue(x.ProductoMaderaId, out var producto) ? producto.PrecioVentaSugerido : 0;
            return x.StockActual * precio;
        });

        var topClientes = ventas
            .GroupBy(x => x.ClienteId)
            .Select(g => new RankingDto(
                clientesMap.TryGetValue(g.Key, out var cliente) ? cliente.NombreRazonSocial : g.Key.ToString(),
                g.Sum(x => x.Total),
                g.Count()))
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        var topProductos = ventas
            .SelectMany(x => x.Detalles)
            .GroupBy(x => x.ProductoMaderaId)
            .Select(g => new RankingDto(
                productosMap.TryGetValue(g.Key, out var producto) ? producto.NombreComercial : g.Key.ToString(),
                g.Sum(x => x.Subtotal),
                g.Sum(x => x.Cantidad)))
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        var periodos = ventas.Select(x => Periodo(x.Fecha))
            .Concat(compras.Select(x => Periodo(x.FechaCompra)))
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        var serie = periodos.Select(periodo => new SeriePeriodoDto(
            periodo,
            ventas.Where(x => Periodo(x.Fecha) == periodo).Sum(x => x.Total),
            compras.Where(x => Periodo(x.FechaCompra) == periodo).Sum(x => x.Detalles.Sum(d => d.Cantidad * d.PrecioCompra) + x.CostoTransporte + x.OtrosCostos)))
            .ToList();

        return new ReporteNegocioDto(r.Desde.Date, r.Hasta.Date, ventasConfirmadas, comprasRecibidas, utilidadBruta, margenPromedio, inventarioValorCosto, inventarioValorVenta, topClientes, topProductos, serie);
    }

    public async Task<List<ProductoHistorialDto>> Handle(ObtenerProductoHistorialQuery r, CancellationToken ct)
    {
        var producto = await uow.Productos.ObtenerPorIdAsync(r.ProductoId, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        var proveedores = (await uow.Proveedores.ListarAsync(ct)).ToDictionary(x => x.Id);
        var clientes = (await uow.Clientes.ListarAsync(ct)).ToDictionary(x => x.Id);
        var compras = await uow.Compras.ListarAsync(ct);
        var ventas = await uow.Ventas.ListarAsync(ct);
        var movimientos = await uow.Inventario.ListarMovimientosAsync(ct);

        var historial = new List<ProductoHistorialDto>();

        historial.AddRange(compras
            .SelectMany(compra => compra.Detalles
                .Where(detalle => detalle.ProductoMaderaId == r.ProductoId)
                .Select(detalle => new ProductoHistorialDto(
                    compra.FechaCompra,
                    "Entrada por compra",
                    compra.Id,
                    $"Compra {ShortId(compra.Id)}",
                    producto.NombreComercial,
                    detalle.Cantidad,
                    detalle.PrecioCompra,
                    detalle.Cantidad * detalle.PrecioCompra,
                    proveedores.TryGetValue(compra.ProveedorId, out var proveedor) ? proveedor.Nombre : compra.ProveedorId.ToString(),
                    $"Origen: {compra.Origen}; estado: {compra.Estado}"))));

        historial.AddRange(ventas
            .Where(venta => venta.Estado != EstadoVenta.Anulada && venta.Estado != EstadoVenta.Pendiente)
            .SelectMany(venta => venta.Detalles
                .Where(detalle => detalle.ProductoMaderaId == r.ProductoId)
                .Select(detalle => new ProductoHistorialDto(
                    venta.Fecha,
                    "Salida por venta",
                    venta.Id,
                    $"Venta {ShortId(venta.Id)}",
                    producto.NombreComercial,
                    -detalle.Cantidad,
                    detalle.PrecioUnitario,
                    detalle.Subtotal,
                    clientes.TryGetValue(venta.ClienteId, out var cliente) ? cliente.NombreRazonSocial : venta.ClienteId.ToString(),
                    $"Estado: {venta.Estado}"))));

        historial.AddRange(movimientos
            .Where(movimiento => movimiento.ProductoMaderaId == r.ProductoId
                && movimiento.Tipo is TipoMovimientoInventario.AjusteManual or TipoMovimientoInventario.Perdida or TipoMovimientoInventario.Devolucion or TipoMovimientoInventario.ReversionVenta)
            .Select(movimiento => new ProductoHistorialDto(
                movimiento.Fecha,
                MovimientoNombre(movimiento.Tipo),
                movimiento.Id,
                $"Movimiento {ShortId(movimiento.Id)}",
                producto.NombreComercial,
                MovimientoCantidadFirmada(movimiento),
                movimiento.CostoUnitario,
                movimiento.Cantidad * movimiento.CostoUnitario,
                null,
                movimiento.Motivo)));

        return historial
            .OrderByDescending(x => x.Fecha)
            .ToList();
    }

    public async Task<List<AnuncioCatalogoDto>> Handle(ListarAnunciosCatalogoQuery r, CancellationToken ct)
    {
        var anuncios = r.SoloPublicados
            ? await uow.AnunciosCatalogo.ListarPublicadosAsync(ct)
            : await uow.AnunciosCatalogo.ListarGestionAsync(ct);
        var inventario = await uow.Inventario.ListarAsync(ct);
        return anuncios.Select(x => x.ToDto(inventario.FirstOrDefault(i => i.ProductoMaderaId == x.ProductoMaderaId))).ToList();
    }

    private static string Periodo(DateTime fecha) => fecha.ToString("yyyy-MM");

    private static string ShortId(Guid id) => id.ToString()[..8];

    private static string MovimientoNombre(TipoMovimientoInventario tipo) => tipo switch
    {
        TipoMovimientoInventario.AjusteManual => "Ajuste manual",
        TipoMovimientoInventario.Perdida => "Perdida",
        TipoMovimientoInventario.Devolucion => "Devolucion",
        TipoMovimientoInventario.ReversionVenta => "Reversion venta",
        _ => tipo.ToString()
    };

    private static decimal MovimientoCantidadFirmada(Sicomoro.Domain.Entities.MovimientoInventario movimiento) =>
        movimiento.Tipo is TipoMovimientoInventario.Perdida ? -movimiento.Cantidad : movimiento.Cantidad;

    private static (DateTime Desde, DateTime Hasta) BoliviaDateRange(DateTime desde, DateTime hasta)
    {
        var tz = GetBoliviaTimeZone();
        var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(desde.Date, DateTimeKind.Unspecified), tz);
        var endLocal = hasta.Date.AddDays(1).AddTicks(-1);
        var end = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), tz);
        return (start, end);
    }

    private static TimeZoneInfo GetBoliviaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time"); }
    }
}
