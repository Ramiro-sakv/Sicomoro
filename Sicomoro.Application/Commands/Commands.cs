using MediatR;
using Sicomoro.Application.DTOs;
using Sicomoro.Application.Interfaces;
using Sicomoro.Application.Validators;
using Sicomoro.Domain.DomainServices;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;
using Sicomoro.Domain.Interfaces;

namespace Sicomoro.Application.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public sealed record RegisterCommand(string Nombre, string Email, string Password, RolSistema Rol) : IRequest<AuthResponse>;

public sealed record CrearClienteCommand(string NombreRazonSocial, string? CiNit, string? Telefono, string? Direccion, string? Ciudad, string? Notas) : IRequest<ClienteDto>;
public sealed record ActualizarClienteCommand(Guid Id, string NombreRazonSocial, string? CiNit, string? Telefono, string? Direccion, string? Ciudad, string? Notas, EstadoRegistro Estado) : IRequest<ClienteDto>;
public sealed record InactivarClienteCommand(Guid Id) : IRequest<bool>;
public sealed record CrearProveedorCommand(string Nombre, string LugarOrigen, string? Telefono, string? Direccion, string? TipoMadera, string? Notas) : IRequest<ProveedorDto>;
public sealed record CrearProductoCommand(string NombreComercial, string TipoMadera, UnidadMedida UnidadMedida, decimal Largo, decimal Ancho, decimal Espesor, string? Calidad, decimal PrecioCompra, decimal PrecioVentaSugerido, decimal StockMinimo, string? Observaciones) : IRequest<ProductoDto>;
public sealed record ActualizarProductoCommand(Guid Id, string NombreComercial, string TipoMadera, UnidadMedida UnidadMedida, decimal Largo, decimal Ancho, decimal Espesor, string? Calidad, decimal PrecioCompra, decimal PrecioVentaSugerido, decimal StockMinimo, EstadoRegistro Estado, string? Observaciones) : IRequest<ProductoDto>;
public sealed record InactivarProductoCommand(Guid Id) : IRequest<bool>;
public sealed record EliminarProductoCommand(Guid Id) : IRequest<bool>;
public sealed record AjustarInventarioCommand(Guid ProductoId, decimal NuevoStock, string? UbicacionInterna, string Motivo) : IRequest<InventarioDto>;
public sealed record CrearTransporteCommand(string? Camion, string? Chofer, string? Placa, string LugarOrigen, DateTime? FechaSalida, DateTime? FechaLlegada, decimal CostoTransporte, EstadoTransporte Estado, string? Observaciones, Guid? CompraId) : IRequest<TransporteDto>;
public sealed record ActualizarEstadoTransporteCommand(Guid Id, EstadoTransporte Estado, DateTime? FechaLlegada) : IRequest<TransporteDto>;
public sealed record CrearCompraCommand(Guid ProveedorId, string Origen, DateTime FechaCompra, DateTime? FechaEstimadaLlegada, decimal CostoTransporte, decimal OtrosCostos, string? Observaciones, IReadOnlyCollection<CompraDetalleInput> Detalles) : IRequest<Guid>;
public sealed record RecibirCompraCommand(Guid CompraId) : IRequest<bool>;
public sealed record CrearVentaCommand(Guid ClienteId, MetodoPago MetodoPago, DateTime? FechaVencimiento, string? Observaciones, IReadOnlyCollection<VentaDetalleInput> Detalles) : IRequest<Guid>;
public sealed record ConfirmarVentaCommand(Guid VentaId, decimal MontoPagado) : IRequest<VentaDto>;
public sealed record AnularVentaCommand(Guid VentaId, string Motivo) : IRequest<bool>;
public sealed record RegistrarPagoCommand(Guid CobroId, decimal Monto, MetodoPago MetodoPago, string? Referencia) : IRequest<CobroDto>;
public sealed record RegistrarCajaMovimientoCommand(TipoCajaMovimiento Tipo, decimal Monto, string Concepto, Guid? CompraId = null) : IRequest<CajaMovimientoDto>;
public sealed record GenerarDocumentoVentaCommand(Guid VentaId, TipoDocumentoVenta Tipo = TipoDocumentoVenta.ComprobanteVenta) : IRequest<DocumentoDto>;
public sealed record EnviarDocumentoVentaCommand(Guid VentaId, string Destino, TipoDocumentoVenta Tipo = TipoDocumentoVenta.ComprobanteVenta) : IRequest<bool>;

public sealed class AuthHandlers(IAuthService authService) :
    IRequestHandler<LoginCommand, AuthResponse>,
    IRequestHandler<RegisterCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request.Email, request.Password, cancellationToken);

    public Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        authService.RegisterAsync(request.Nombre, request.Email, request.Password, request.Rol, cancellationToken);
}

public sealed class CatalogoHandlers(IUnitOfWork uow) :
    IRequestHandler<CrearClienteCommand, ClienteDto>,
    IRequestHandler<ActualizarClienteCommand, ClienteDto>,
    IRequestHandler<InactivarClienteCommand, bool>,
    IRequestHandler<CrearProveedorCommand, ProveedorDto>,
    IRequestHandler<CrearProductoCommand, ProductoDto>,
    IRequestHandler<ActualizarProductoCommand, ProductoDto>,
    IRequestHandler<InactivarProductoCommand, bool>,
    IRequestHandler<EliminarProductoCommand, bool>,
    IRequestHandler<CrearTransporteCommand, TransporteDto>,
    IRequestHandler<ActualizarEstadoTransporteCommand, TransporteDto>
{
    public async Task<ClienteDto> Handle(CrearClienteCommand r, CancellationToken ct)
    {
        var cliente = new Cliente(r.NombreRazonSocial, r.CiNit, r.Telefono, r.Direccion, r.Ciudad, r.Notas);
        await uow.Clientes.AgregarAsync(cliente, ct);
        await uow.SaveChangesAsync(ct);
        return cliente.ToDto(0);
    }

    public async Task<ClienteDto> Handle(ActualizarClienteCommand r, CancellationToken ct)
    {
        var cliente = await uow.Clientes.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        cliente.Actualizar(r.NombreRazonSocial, r.CiNit, r.Telefono, r.Direccion, r.Ciudad, r.Notas, r.Estado);
        await uow.SaveChangesAsync(ct);
        var deuda = await uow.Clientes.ObtenerDeudaTotalAsync(cliente.Id, ct);
        return cliente.ToDto(deuda);
    }

    public async Task<ProveedorDto> Handle(CrearProveedorCommand r, CancellationToken ct)
    {
        var proveedor = new Proveedor(r.Nombre, r.LugarOrigen, r.Telefono, r.Direccion, r.TipoMadera, r.Notas);
        await uow.Proveedores.AgregarAsync(proveedor, ct);
        await uow.SaveChangesAsync(ct);
        return proveedor.ToDto();
    }

    public async Task<bool> Handle(InactivarClienteCommand r, CancellationToken ct)
    {
        var cliente = await uow.Clientes.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        cliente.Actualizar(cliente.NombreRazonSocial, cliente.CiNit, cliente.Telefono, cliente.Direccion, cliente.Ciudad, cliente.Notas, EstadoRegistro.Inactivo);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ProductoDto> Handle(CrearProductoCommand r, CancellationToken ct)
    {
        var producto = new ProductoMadera(r.NombreComercial, r.TipoMadera, r.UnidadMedida, r.Largo, r.Ancho, r.Espesor, r.Calidad, r.PrecioCompra, r.PrecioVentaSugerido, r.StockMinimo, r.Observaciones);
        await uow.Productos.AgregarAsync(producto, ct);
        await uow.Inventario.AgregarAsync(new Inventario(producto.Id, null), ct);
        await uow.SaveChangesAsync(ct);
        return producto.ToDto();
    }

    public async Task<ProductoDto> Handle(ActualizarProductoCommand r, CancellationToken ct)
    {
        var producto = await uow.Productos.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        producto.Actualizar(r.NombreComercial, r.TipoMadera, r.UnidadMedida, r.Largo, r.Ancho, r.Espesor, r.Calidad, r.PrecioCompra, r.PrecioVentaSugerido, r.StockMinimo, r.Estado, r.Observaciones);
        await uow.SaveChangesAsync(ct);
        return producto.ToDto();
    }

    public async Task<bool> Handle(InactivarProductoCommand r, CancellationToken ct)
    {
        var producto = await uow.Productos.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        producto.Inactivar();
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(EliminarProductoCommand r, CancellationToken ct)
    {
        var producto = await uow.Productos.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        if (await uow.Productos.TieneHistorialAsync(r.Id, ct))
        {
            throw new InvalidOperationException("No se puede borrar definitivamente un producto con compras, ventas o movimientos de inventario. Marcalo como Inactivo para ocultarlo de nuevas operaciones sin perder historial.");
        }

        await uow.Productos.EliminarDefinitivoAsync(producto, ct);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TransporteDto> Handle(CrearTransporteCommand r, CancellationToken ct)
    {
        var transporte = new Transporte(r.Camion, r.Chofer, r.Placa, r.LugarOrigen, r.FechaSalida, r.FechaLlegada, r.CostoTransporte, r.Estado, r.Observaciones, r.CompraId);
        await uow.Transportes.AgregarAsync(transporte, ct);
        await uow.SaveChangesAsync(ct);
        return transporte.ToDto();
    }

    public async Task<TransporteDto> Handle(ActualizarEstadoTransporteCommand r, CancellationToken ct)
    {
        var transporte = await uow.Transportes.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Transporte no encontrado.");
        transporte.ActualizarEstado(r.Estado, r.FechaLlegada);
        await uow.SaveChangesAsync(ct);
        return transporte.ToDto();
    }
}

public sealed class AjustarInventarioHandler(IUnitOfWork uow, ICurrentUserService currentUser) : IRequestHandler<AjustarInventarioCommand, InventarioDto>
{
    public async Task<InventarioDto> Handle(AjustarInventarioCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        var producto = await uow.Productos.ObtenerPorIdAsync(r.ProductoId, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        var inventario = await uow.Inventario.ObtenerPorProductoAsync(r.ProductoId, ct);
        if (inventario is null)
        {
            inventario = new Inventario(r.ProductoId, r.UbicacionInterna);
            await uow.Inventario.AgregarAsync(inventario, ct);
        }
        inventario.Ajustar(r.NuevoStock, r.UbicacionInterna);
        await uow.Inventario.AgregarMovimientoAsync(new MovimientoInventario(r.ProductoId, usuarioId, TipoMovimientoInventario.AjusteManual, r.NuevoStock, producto.PrecioCompra, r.Motivo), ct);
        await uow.SaveChangesAsync(ct);
        return inventario.ToDto(producto);
    }
}

public sealed class CompraHandlers(IUnitOfWork uow, ICurrentUserService currentUser) :
    IRequestHandler<CrearCompraCommand, Guid>,
    IRequestHandler<RecibirCompraCommand, bool>
{
    public async Task<Guid> Handle(CrearCompraCommand r, CancellationToken ct)
    {
        if (r.Detalles.Count == 0) throw new InvalidOperationException("La compra debe tener detalle.");
        var compra = new Compra(r.ProveedorId, r.Origen, r.FechaCompra, r.FechaEstimadaLlegada, r.CostoTransporte, r.OtrosCostos, r.Observaciones);
        foreach (var d in r.Detalles) compra.AgregarDetalle(d.ProductoId, d.Cantidad, d.PrecioCompra);
        await uow.Compras.AgregarAsync(compra, ct);
        await uow.SaveChangesAsync(ct);
        return compra.Id;
    }

    public async Task<bool> Handle(RecibirCompraCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        await using var tx = await uow.BeginTransactionAsync(ct);
        var compra = await uow.Compras.ObtenerConDetallesAsync(r.CompraId, ct) ?? throw new KeyNotFoundException("Compra no encontrada.");
        compra.Recibir();
        foreach (var detalle in compra.Detalles)
        {
            var inventario = await uow.Inventario.ObtenerPorProductoAsync(detalle.ProductoMaderaId, ct);
            if (inventario is null)
            {
                inventario = new Inventario(detalle.ProductoMaderaId, null);
                await uow.Inventario.AgregarAsync(inventario, ct);
            }
            inventario.Incrementar(detalle.Cantidad);
            await uow.Inventario.AgregarMovimientoAsync(new MovimientoInventario(detalle.ProductoMaderaId, usuarioId, TipoMovimientoInventario.EntradaCompra, detalle.Cantidad, detalle.PrecioCompra, "Compra recibida", compraId: compra.Id), ct);
        }
        await uow.AgregarAsync(new CajaMovimiento(TipoCajaMovimiento.Egreso, compra.CostoTransporte + compra.OtrosCostos, "Costos de compra/transporte", usuarioId, compraId: compra.Id), ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}

public sealed class VentaHandlers(IUnitOfWork uow, ICurrentUserService currentUser, PricingService pricing, VentaValidationChain validationChain, EstadoVentaFactory estadoFactory) :
    IRequestHandler<CrearVentaCommand, Guid>,
    IRequestHandler<ConfirmarVentaCommand, VentaDto>,
    IRequestHandler<AnularVentaCommand, bool>
{
    public async Task<Guid> Handle(CrearVentaCommand r, CancellationToken ct)
    {
        if (r.Detalles.Count == 0) throw new InvalidOperationException("No se puede confirmar una venta sin detalle.");
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        var venta = new Venta(r.ClienteId, usuarioId, r.MetodoPago, r.FechaVencimiento, r.Observaciones);
        foreach (var detalle in r.Detalles)
        {
            var producto = await uow.Productos.ObtenerPorIdAsync(detalle.ProductoId, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
            var precio = detalle.PrecioUnitario ?? pricing.Calcular(detalle.PricingStrategy, producto, detalle.Cantidad, detalle.Descuento);
            venta.AgregarDetalle(detalle.ProductoId, detalle.Cantidad, precio, detalle.Descuento);
        }
        await uow.Ventas.AgregarAsync(venta, ct);
        await uow.SaveChangesAsync(ct);
        return venta.Id;
    }

    public async Task<VentaDto> Handle(ConfirmarVentaCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        await using var tx = await uow.BeginTransactionAsync(ct);
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        await validationChain.ValidarAsync(venta, currentUser.Rol, ct);
        venta.Confirmar(r.MontoPagado);
        foreach (var detalle in venta.Detalles)
        {
            var inventario = await uow.Inventario.ObtenerPorProductoAsync(detalle.ProductoMaderaId, ct) ?? throw new InvalidOperationException("Producto sin inventario.");
            inventario.Descontar(detalle.Cantidad);
            await uow.Inventario.AgregarMovimientoAsync(new MovimientoInventario(detalle.ProductoMaderaId, usuarioId, TipoMovimientoInventario.SalidaVenta, detalle.Cantidad, detalle.PrecioUnitario, "Venta confirmada", ventaId: venta.Id), ct);
        }
        if (r.MontoPagado > 0)
            await uow.AgregarAsync(new CajaMovimiento(TipoCajaMovimiento.Ingreso, r.MontoPagado, "Cobro inicial de venta", usuarioId, ventaId: venta.Id), ct);
        if (venta.SaldoPendiente > 0)
            await uow.Cobros.AgregarAsync(new Cobro(venta.Id, venta.ClienteId, venta.SaldoPendiente, venta.FechaVencimiento), ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return venta.ToDto();
    }

    public async Task<bool> Handle(AnularVentaCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        await using var tx = await uow.BeginTransactionAsync(ct);
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        estadoFactory.Crear(venta.Estado).ValidarPermiteAnulacion();
        var debeRevertir = venta.Estado != EstadoVenta.Pendiente;
        venta.Anular(r.Motivo);
        if (debeRevertir)
        {
            foreach (var detalle in venta.Detalles)
            {
                var inventario = await uow.Inventario.ObtenerPorProductoAsync(detalle.ProductoMaderaId, ct) ?? throw new InvalidOperationException("Producto sin inventario.");
                inventario.Incrementar(detalle.Cantidad);
                await uow.Inventario.AgregarMovimientoAsync(new MovimientoInventario(detalle.ProductoMaderaId, usuarioId, TipoMovimientoInventario.ReversionVenta, detalle.Cantidad, detalle.PrecioUnitario, "Venta anulada", ventaId: venta.Id), ct);
            }
        }
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}

public sealed class CobroHandler(IUnitOfWork uow, ICurrentUserService currentUser, EstadoVentaFactory estadoFactory) : IRequestHandler<RegistrarPagoCommand, CobroDto>
{
    public async Task<CobroDto> Handle(RegistrarPagoCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        await using var tx = await uow.BeginTransactionAsync(ct);
        var cobro = await uow.Cobros.ObtenerPorIdAsync(r.CobroId, ct) ?? throw new KeyNotFoundException("Cobro no encontrado.");
        var venta = await uow.Ventas.ObtenerPorIdAsync(cobro.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        estadoFactory.Crear(venta.Estado).ValidarPermitePago();
        var pago = cobro.RegistrarPago(r.Monto, r.MetodoPago, usuarioId, r.Referencia);
        await uow.AgregarAsync(pago, ct);
        venta.RegistrarPago(r.Monto);
        await uow.AgregarAsync(new CajaMovimiento(TipoCajaMovimiento.Ingreso, r.Monto, "Pago de cuenta por cobrar", usuarioId, ventaId: venta.Id, pagoId: pago.Id), ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return cobro.ToDto();
    }
}

public sealed class CajaHandler(IUnitOfWork uow, ICurrentUserService currentUser) : IRequestHandler<RegistrarCajaMovimientoCommand, CajaMovimientoDto>
{
    public async Task<CajaMovimientoDto> Handle(RegistrarCajaMovimientoCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        if (r.Monto <= 0) throw new InvalidOperationException("El monto debe ser mayor a cero.");
        var movimiento = new CajaMovimiento(r.Tipo, r.Monto, r.Concepto, usuarioId, compraId: r.CompraId);
        await uow.Caja.AgregarAsync(movimiento, ct);
        await uow.SaveChangesAsync(ct);
        return movimiento.ToDto();
    }
}

public sealed class DocumentoHandlers(IUnitOfWork uow, ICurrentUserService currentUser, IDocumentoFactory factory) :
    IRequestHandler<GenerarDocumentoVentaCommand, DocumentoDto>,
    IRequestHandler<EnviarDocumentoVentaCommand, bool>
{
    public async Task<DocumentoDto> Handle(GenerarDocumentoVentaCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        var documento = await factory.Crear(r.Tipo).GenerarDocumentoVentaAsync(venta, usuarioId, ct);
        await uow.AgregarAsync(documento, ct);
        await uow.SaveChangesAsync(ct);
        return documento.ToDto();
    }

    public async Task<bool> Handle(EnviarDocumentoVentaCommand r, CancellationToken ct)
    {
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        var documento = await factory.Crear(r.Tipo).GenerarDocumentoVentaAsync(venta, currentUser.UserId ?? Guid.Empty, ct);
        await factory.Crear(r.Tipo).EnviarDocumentoAsync(documento, r.Destino, ct);
        return true;
    }
}

public static class MappingExtensions
{
    public static ClienteDto ToDto(this Cliente x, decimal deuda) => new(x.Id, x.NombreRazonSocial, x.CiNit, x.Telefono, x.Direccion, x.Ciudad, x.Notas, x.Estado, deuda);
    public static ProveedorDto ToDto(this Proveedor x) => new(x.Id, x.Nombre, x.LugarOrigen, x.Telefono, x.Direccion, x.TipoMadera, x.Notas);
    public static ProductoDto ToDto(this ProductoMadera x) => new(x.Id, x.NombreComercial, x.TipoMadera, x.UnidadMedida, x.Largo, x.Ancho, x.Espesor, x.Calidad, x.PrecioCompra, x.PrecioVentaSugerido, x.StockMinimo, x.Estado);
    public static InventarioDto ToDto(this Inventario x, ProductoMadera producto) => new(x.Id, x.ProductoMaderaId, producto.NombreComercial, x.StockActual, producto.StockMinimo, x.UbicacionInterna);
    public static TransporteDto ToDto(this Transporte x) => new(x.Id, x.Camion, x.Chofer, x.Placa, x.LugarOrigen, x.FechaSalida, x.FechaLlegada, x.CostoTransporte, x.Estado, x.Observaciones, x.CompraId);
    public static VentaDto ToDto(this Venta x) => new(x.Id, x.ClienteId, x.Fecha, x.Estado, x.Total, x.MontoPagado, x.SaldoPendiente);
    public static CobroDto ToDto(this Cobro x) => new(x.Id, x.VentaId, x.ClienteId, x.MontoTotal, x.SaldoPendiente, x.Estado, x.FechaVencimiento);
    public static DocumentoDto ToDto(this DocumentoVenta x) => new(x.Id, x.VentaId, x.Tipo, x.Numero, x.RutaArchivo, x.FechaGeneracion);
    public static CajaMovimientoDto ToDto(this CajaMovimiento x) => new(x.Id, x.Fecha, x.Tipo, x.Monto, x.Concepto, x.UsuarioId, x.VentaId, x.PagoId, x.CompraId);
    public static NotificacionDto ToDto(this Notificacion x) => new(x.Id, x.Tipo, x.Titulo, x.Mensaje, x.UsuarioId, x.Leida, x.CreadoEn);
    public static AuditoriaDto ToDto(this Auditoria x) => new(x.Id, x.UsuarioId, x.FechaHora, x.Accion, x.Entidad, x.EntidadId, x.DatosAntes, x.DatosDespues);
}
