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
public sealed record RegisterCommand(string Nombre, string Email, string Password, RolSistema Rol, string? ClaveCreacion = null, string? CiNit = null, string? Telefono = null, string? Direccion = null, string? Cargo = null, string? Notas = null) : IRequest<AuthResponse>;
public sealed record LoginClientePortalCommand(string Email, string Password) : IRequest<AuthResponse>;
public sealed record RegistrarClientePortalCommand(string Nombre, string Email, string Password, string? CiNit = null, string? Telefono = null, string? Direccion = null, string? Ciudad = null) : IRequest<AuthResponse>;
public sealed record CrearUsuarioCommand(string Nombre, string Email, string Password, RolSistema Rol, string ClaveCreacion, string? CiNit = null, string? Telefono = null, string? Direccion = null, string? Cargo = null, string? Notas = null) : IRequest<UsuarioDto>;
public sealed record EliminarUsuarioPorEmailCommand(string Email, string? ClaveOperacion = null) : IRequest<bool>;
public sealed record EliminarUsuarioCommand(Guid Id, string? ClaveOperacion = null) : IRequest<bool>;
public sealed record ResetearUsuarioPasswordCommand(Guid Id, string NuevaPassword) : IRequest<bool>;
public sealed record ActualizarMiPerfilCommand(string Nombre, string Email, string? CiNit, string? Telefono, string? Direccion, string? Cargo, string? Notas) : IRequest<UsuarioDto>;
public sealed record CambiarMiPasswordCommand(string PasswordActual, string NuevaPassword) : IRequest<bool>;

public sealed record CrearClienteCommand(string NombreRazonSocial, string? CiNit, string? Telefono, string? Direccion, string? Ciudad, string? Notas) : IRequest<ClienteDto>;
public sealed record ActualizarClienteCommand(Guid Id, string NombreRazonSocial, string? CiNit, string? Telefono, string? Direccion, string? Ciudad, string? Notas, EstadoRegistro Estado) : IRequest<ClienteDto>;
public sealed record EliminarClienteCommand(Guid Id, string? ClaveOperacion = null) : IRequest<bool>;
public sealed record CrearProveedorCommand(string Nombre, string LugarOrigen, string? Telefono, string? Direccion, string? TipoMadera, string? Notas) : IRequest<ProveedorDto>;
public sealed record EliminarProveedorCommand(Guid Id, string? ClaveOperacion = null) : IRequest<bool>;
public sealed record CrearProductoCommand(string NombreComercial, string TipoMadera, UnidadMedida UnidadMedida, decimal Largo, decimal Ancho, decimal Espesor, string? Calidad, decimal PrecioCompra, decimal PrecioVentaSugerido, decimal StockMinimo, string? Observaciones) : IRequest<ProductoDto>;
public sealed record ActualizarProductoCommand(Guid Id, string NombreComercial, string TipoMadera, UnidadMedida UnidadMedida, decimal Largo, decimal Ancho, decimal Espesor, string? Calidad, decimal PrecioCompra, decimal PrecioVentaSugerido, decimal StockMinimo, EstadoRegistro Estado, string? Observaciones) : IRequest<ProductoDto>;
public sealed record InactivarProductoCommand(Guid Id) : IRequest<bool>;
public sealed record EliminarProductoCommand(Guid Id, string? ClaveOperacion = null) : IRequest<bool>;
public sealed record AjustarInventarioCommand(Guid ProductoId, decimal NuevoStock, string? UbicacionInterna, string Motivo) : IRequest<InventarioDto>;
public sealed record CrearTransporteCommand(string? Camion, string? Chofer, string? Placa, string LugarOrigen, DateTime? FechaSalida, DateTime? FechaLlegada, decimal CostoTransporte, EstadoTransporte Estado, string? Observaciones, Guid? CompraId) : IRequest<TransporteDto>;
public sealed record ActualizarEstadoTransporteCommand(Guid Id, EstadoTransporte Estado, DateTime? FechaLlegada) : IRequest<TransporteDto>;
public sealed record CrearCompraCommand(Guid ProveedorId, string Origen, DateTime FechaCompra, DateTime? FechaEstimadaLlegada, decimal CostoTransporte, decimal OtrosCostos, string? Observaciones, IReadOnlyCollection<CompraDetalleInput> Detalles) : IRequest<Guid>;
public sealed record ActualizarCompraCommand(Guid CompraId, Guid ProveedorId, string Origen, DateTime FechaCompra, DateTime? FechaEstimadaLlegada, decimal CostoTransporte, decimal OtrosCostos, string? Observaciones, IReadOnlyCollection<CompraDetalleInput> Detalles) : IRequest<CompraDto>;
public sealed record RecibirCompraCommand(Guid CompraId) : IRequest<bool>;
public sealed record CrearVentaCommand(Guid ClienteId, MetodoPago MetodoPago, DateTime? FechaVencimiento, string? Observaciones, IReadOnlyCollection<VentaDetalleInput> Detalles) : IRequest<Guid>;
public sealed record ActualizarVentaCommand(Guid VentaId, Guid ClienteId, MetodoPago MetodoPago, DateTime? FechaVencimiento, string? Observaciones, IReadOnlyCollection<VentaDetalleInput> Detalles) : IRequest<VentaDto>;
public sealed record ConfirmarVentaCommand(Guid VentaId, decimal MontoPagado) : IRequest<VentaDto>;
public sealed record AnularVentaCommand(Guid VentaId, string Motivo) : IRequest<bool>;
public sealed record RegistrarPagoCommand(Guid CobroId, decimal Monto, MetodoPago MetodoPago, string? Referencia) : IRequest<CobroDto>;
public sealed record RegistrarCajaMovimientoCommand(TipoCajaMovimiento Tipo, decimal Monto, string Concepto, Guid? CompraId = null) : IRequest<CajaMovimientoDto>;
public sealed record GenerarDocumentoVentaCommand(Guid VentaId, TipoDocumentoVenta Tipo = TipoDocumentoVenta.ComprobanteVenta) : IRequest<DocumentoDto>;
public sealed record EnviarDocumentoVentaCommand(Guid VentaId, string Destino, TipoDocumentoVenta Tipo = TipoDocumentoVenta.ComprobanteVenta) : IRequest<bool>;
public sealed record CrearAnuncioCatalogoCommand(Guid? ProductoId, string Titulo, string? Subtitulo, string Descripcion, string? ImagenUrl, string? PrecioTexto, string? Etiqueta, string? CtaTexto, string? CtaUrl, int Orden, bool Publicado) : IRequest<AnuncioCatalogoDto>;
public sealed record ActualizarAnuncioCatalogoCommand(Guid Id, Guid? ProductoId, string Titulo, string? Subtitulo, string Descripcion, string? ImagenUrl, string? PrecioTexto, string? Etiqueta, string? CtaTexto, string? CtaUrl, int Orden, bool Publicado) : IRequest<AnuncioCatalogoDto>;
public sealed record EliminarAnuncioCatalogoCommand(Guid Id, string? ClaveOperacion = null) : IRequest<bool>;

public sealed class AuthHandlers(IAuthService authService, IUnitOfWork uow, ICurrentUserService currentUser, IPasswordHasher hasher, IUserCreationKeyValidator creationKeyValidator) :
    IRequestHandler<LoginCommand, AuthResponse>,
    IRequestHandler<RegisterCommand, AuthResponse>,
    IRequestHandler<LoginClientePortalCommand, AuthResponse>,
    IRequestHandler<RegistrarClientePortalCommand, AuthResponse>,
    IRequestHandler<CrearUsuarioCommand, UsuarioDto>,
    IRequestHandler<EliminarUsuarioPorEmailCommand, bool>,
    IRequestHandler<EliminarUsuarioCommand, bool>,
    IRequestHandler<ResetearUsuarioPasswordCommand, bool>,
    IRequestHandler<ActualizarMiPerfilCommand, UsuarioDto>,
    IRequestHandler<CambiarMiPasswordCommand, bool>
{
    public Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request.Email, request.Password, cancellationToken);

    public Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!creationKeyValidator.IsValid(request.ClaveCreacion))
            throw new UnauthorizedAccessException("Clave de creacion de usuario invalida.");

        return authService.RegisterAsync(request.Nombre, request.Email, request.Password, request.Rol, request.CiNit, request.Telefono, request.Direccion, request.Cargo, request.Notas, cancellationToken);
    }

    public async Task<AuthResponse> Handle(LoginClientePortalCommand request, CancellationToken cancellationToken)
    {
        var auth = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (auth.Rol != RolSistema.SoloLectura)
            throw new UnauthorizedAccessException("Esta cuenta pertenece al personal. Usa el acceso interno.");
        return auth;
    }

    public async Task<AuthResponse> Handle(RegistrarClientePortalCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new InvalidOperationException("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("El email es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new InvalidOperationException("La contrasena debe tener al menos 8 caracteres.");
        if (await uow.Usuarios.ObtenerPorEmailAsync(request.Email, cancellationToken) is not null)
            throw new InvalidOperationException("El email ya esta registrado.");

        await using var tx = await uow.BeginTransactionAsync(cancellationToken);
        var usuario = new Usuario(request.Nombre, request.Email, hasher.Hash(request.Password), RolSistema.SoloLectura, request.CiNit, request.Telefono, request.Direccion, "Cliente", "Cuenta creada desde catalogo publico.");
        var cliente = new Cliente(request.Nombre, request.CiNit, request.Telefono, request.Direccion, request.Ciudad, $"Cuenta portal: {request.Email.Trim().ToLowerInvariant()}");
        await uow.Usuarios.AgregarAsync(usuario, cancellationToken);
        await uow.Clientes.AgregarAsync(cliente, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return await authService.LoginAsync(request.Email, request.Password, cancellationToken);
    }

    public async Task<UsuarioDto> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        if (!creationKeyValidator.IsValid(request.ClaveCreacion))
            throw new UnauthorizedAccessException("Clave de creacion de usuario invalida.");

        if (await uow.Usuarios.ObtenerPorEmailAsync(request.Email, cancellationToken) is not null)
            throw new InvalidOperationException("El email ya esta registrado.");

        var usuario = new Usuario(request.Nombre, request.Email, hasher.Hash(request.Password), request.Rol, request.CiNit, request.Telefono, request.Direccion, request.Cargo, request.Notas);
        await uow.Usuarios.AgregarAsync(usuario, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return usuario.ToDto();
    }

    public async Task<bool> Handle(EliminarUsuarioPorEmailCommand request, CancellationToken cancellationToken)
    {
        ValidarClaveOperacion(request.ClaveOperacion);
        var usuario = await uow.Usuarios.ObtenerPorEmailAsync(request.Email, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        await EliminarUsuarioAsync(usuario, cancellationToken);
        return true;
    }

    public async Task<bool> Handle(EliminarUsuarioCommand request, CancellationToken cancellationToken)
    {
        ValidarClaveOperacion(request.ClaveOperacion);
        var usuario = await uow.Usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        await EliminarUsuarioAsync(usuario, cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ResetearUsuarioPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NuevaPassword) || request.NuevaPassword.Length < 8)
            throw new InvalidOperationException("La nueva contrasena debe tener al menos 8 caracteres.");

        var usuario = await uow.Usuarios.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        usuario.CambiarPassword(hasher.Hash(request.NuevaPassword));
        await uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UsuarioDto> Handle(ActualizarMiPerfilCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Sesion invalida.");
        var usuario = await uow.Usuarios.ObtenerPorIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var existente = await uow.Usuarios.ObtenerPorEmailAsync(request.Email, cancellationToken);
        if (existente is not null && existente.Id != usuario.Id)
            throw new InvalidOperationException("El email ya esta registrado por otro usuario.");

        usuario.ActualizarPerfil(request.Nombre, request.Email, request.CiNit, request.Telefono, request.Direccion, request.Cargo, request.Notas);
        await uow.SaveChangesAsync(cancellationToken);
        return usuario.ToDto();
    }

    public async Task<bool> Handle(CambiarMiPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NuevaPassword) || request.NuevaPassword.Length < 8)
            throw new InvalidOperationException("La nueva contrasena debe tener al menos 8 caracteres.");

        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Sesion invalida.");
        var usuario = await uow.Usuarios.ObtenerPorIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!hasher.Verify(request.PasswordActual, usuario.PasswordHash))
            throw new UnauthorizedAccessException("La contrasena actual no es correcta.");

        usuario.CambiarPassword(hasher.Hash(request.NuevaPassword));
        await uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EliminarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == usuario.Id)
            throw new InvalidOperationException("No puedes eliminar tu propio usuario desde la sesion actual.");

        if (usuario.Rol == RolSistema.Administrador && await uow.Usuarios.ContarAdministradoresAsync(cancellationToken) <= 1)
            throw new InvalidOperationException("No se puede eliminar el ultimo administrador.");

        uow.Usuarios.Eliminar(usuario);
        await uow.SaveChangesAsync(cancellationToken);
    }

    private void ValidarClaveOperacion(string? clave)
    {
        if (!creationKeyValidator.IsValid(clave))
            throw new UnauthorizedAccessException("Clave de operacion invalida.");
    }
}

public sealed class CatalogoHandlers(IUnitOfWork uow, IUserCreationKeyValidator operationKeyValidator) :
    IRequestHandler<CrearClienteCommand, ClienteDto>,
    IRequestHandler<ActualizarClienteCommand, ClienteDto>,
    IRequestHandler<EliminarClienteCommand, bool>,
    IRequestHandler<CrearProveedorCommand, ProveedorDto>,
    IRequestHandler<EliminarProveedorCommand, bool>,
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

    public async Task<bool> Handle(EliminarClienteCommand r, CancellationToken ct)
    {
        ValidarClaveOperacion(r.ClaveOperacion);
        var cliente = await uow.Clientes.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        if (await uow.Clientes.TieneHistorialAsync(r.Id, ct))
            throw new InvalidOperationException("No se puede borrar definitivamente un cliente con ventas o cobros registrados. Marcalo como Inactivo para conservar el historial.");

        uow.Clientes.Eliminar(cliente);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(EliminarProveedorCommand r, CancellationToken ct)
    {
        ValidarClaveOperacion(r.ClaveOperacion);
        var proveedor = await uow.Proveedores.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Proveedor no encontrado.");
        if (await uow.Proveedores.TieneHistorialAsync(r.Id, ct))
            throw new InvalidOperationException("No se puede borrar definitivamente un proveedor con compras registradas. Conserva el proveedor para mantener la trazabilidad.");

        uow.Proveedores.Eliminar(proveedor);
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
        ValidarClaveOperacion(r.ClaveOperacion);
        var producto = await uow.Productos.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
        if (await uow.Productos.TieneHistorialAsync(r.Id, ct))
        {
            throw new InvalidOperationException("No se puede borrar definitivamente un producto con compras, ventas o movimientos de inventario. Marcalo como Inactivo para ocultarlo de nuevas operaciones sin perder historial.");
        }

        await uow.Productos.EliminarDefinitivoAsync(producto, ct);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    private void ValidarClaveOperacion(string? clave)
    {
        if (!operationKeyValidator.IsValid(clave))
            throw new UnauthorizedAccessException("Clave de operacion invalida.");
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

public sealed record EnviarWhatsAppPruebaCommand(string Mensaje) : IRequest<bool>;

public sealed class WhatsAppPruebaHandler(IBusinessAlertService alertas) : IRequestHandler<EnviarWhatsAppPruebaCommand, bool>
{
    public async Task<bool> Handle(EnviarWhatsAppPruebaCommand request, CancellationToken cancellationToken)
    {
        await alertas.EnviarPruebaAsync(request.Mensaje, cancellationToken);
        return true;
    }
}

public sealed class AjustarInventarioHandler(IUnitOfWork uow, ICurrentUserService currentUser, IBusinessAlertService alertas) : IRequestHandler<AjustarInventarioCommand, InventarioDto>
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
        await alertas.InventarioAjustadoAsync(producto.NombreComercial, inventario.StockActual, producto.StockMinimo, r.Motivo, ct);
        return inventario.ToDto(producto);
    }
}

public sealed class CompraHandlers(IUnitOfWork uow, ICurrentUserService currentUser, IBusinessAlertService alertas) :
    IRequestHandler<CrearCompraCommand, Guid>,
    IRequestHandler<ActualizarCompraCommand, CompraDto>,
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

    public async Task<CompraDto> Handle(ActualizarCompraCommand r, CancellationToken ct)
    {
        if (r.Detalles.Count == 0) throw new InvalidOperationException("La compra debe tener detalle.");
        var compra = await uow.Compras.ObtenerConDetallesAsync(r.CompraId, ct) ?? throw new KeyNotFoundException("Compra no encontrada.");
        compra.ActualizarPendiente(r.ProveedorId, r.Origen, r.FechaCompra, r.FechaEstimadaLlegada, r.CostoTransporte, r.OtrosCostos, r.Observaciones);
        compra.LimpiarDetallesPendiente();
        foreach (var d in r.Detalles) compra.AgregarDetalle(d.ProductoId, d.Cantidad, d.PrecioCompra);
        await uow.SaveChangesAsync(ct);
        return compra.ToDto();
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
        var proveedor = await uow.Proveedores.ObtenerPorIdAsync(compra.ProveedorId, ct);
        await alertas.CompraRecibidaAsync(compra.Id, proveedor?.Nombre ?? compra.ProveedorId.ToString(), compra.Origen, compra.Detalles.Sum(x => x.Cantidad * x.PrecioCompra), ct);
        return true;
    }
}

public sealed class VentaHandlers(IUnitOfWork uow, ICurrentUserService currentUser, PricingService pricing, VentaValidationChain validationChain, EstadoVentaFactory estadoFactory, IBusinessAlertService alertas) :
    IRequestHandler<CrearVentaCommand, Guid>,
    IRequestHandler<ActualizarVentaCommand, VentaDto>,
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

    public async Task<VentaDto> Handle(ActualizarVentaCommand r, CancellationToken ct)
    {
        if (r.Detalles.Count == 0) throw new InvalidOperationException("La venta debe tener detalle.");
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        venta.ActualizarPendiente(r.ClienteId, r.MetodoPago, r.FechaVencimiento, r.Observaciones);
        venta.LimpiarDetallesPendiente();
        foreach (var detalle in r.Detalles)
        {
            var producto = await uow.Productos.ObtenerPorIdAsync(detalle.ProductoId, ct) ?? throw new KeyNotFoundException("Producto no encontrado.");
            var precio = detalle.PrecioUnitario ?? pricing.Calcular(detalle.PricingStrategy, producto, detalle.Cantidad, detalle.Descuento);
            venta.AgregarDetalle(detalle.ProductoId, detalle.Cantidad, precio, detalle.Descuento);
        }
        await uow.SaveChangesAsync(ct);
        return venta.ToDto();
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
        var cliente = await uow.Clientes.ObtenerPorIdAsync(venta.ClienteId, ct);
        await alertas.VentaConfirmadaAsync(venta.Id, cliente?.NombreRazonSocial ?? venta.ClienteId.ToString(), venta.Total, venta.MontoPagado, venta.SaldoPendiente, ct);
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
        await alertas.VentaAnuladaAsync(venta.Id, r.Motivo, ct);
        return true;
    }
}

public sealed class CobroHandler(IUnitOfWork uow, ICurrentUserService currentUser, EstadoVentaFactory estadoFactory, IBusinessAlertService alertas) : IRequestHandler<RegistrarPagoCommand, CobroDto>
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
        var cliente = await uow.Clientes.ObtenerPorIdAsync(venta.ClienteId, ct);
        await alertas.PagoRegistradoAsync(venta.Id, cliente?.NombreRazonSocial ?? venta.ClienteId.ToString(), r.Monto, cobro.SaldoPendiente, ct);
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

public sealed class CatalogoPublicoHandlers(IUnitOfWork uow, IUserCreationKeyValidator operationKeyValidator) :
    IRequestHandler<CrearAnuncioCatalogoCommand, AnuncioCatalogoDto>,
    IRequestHandler<ActualizarAnuncioCatalogoCommand, AnuncioCatalogoDto>,
    IRequestHandler<EliminarAnuncioCatalogoCommand, bool>
{
    public async Task<AnuncioCatalogoDto> Handle(CrearAnuncioCatalogoCommand r, CancellationToken ct)
    {
        ValidarAnuncio(r.Titulo, r.Descripcion);
        await ValidarProductoAsync(r.ProductoId, ct);
        var anuncio = new AnuncioCatalogo(r.ProductoId, r.Titulo, r.Subtitulo, r.Descripcion, r.ImagenUrl, r.PrecioTexto, r.Etiqueta, r.CtaTexto, r.CtaUrl, r.Orden, r.Publicado);
        await uow.AnunciosCatalogo.AgregarAsync(anuncio, ct);
        await uow.SaveChangesAsync(ct);
        var inventario = r.ProductoId is null ? null : await uow.Inventario.ObtenerPorProductoAsync(r.ProductoId.Value, ct);
        return anuncio.ToDto(inventario);
    }

    public async Task<AnuncioCatalogoDto> Handle(ActualizarAnuncioCatalogoCommand r, CancellationToken ct)
    {
        ValidarAnuncio(r.Titulo, r.Descripcion);
        await ValidarProductoAsync(r.ProductoId, ct);
        var anuncio = await uow.AnunciosCatalogo.ObtenerConProductoAsync(r.Id, ct) ?? throw new KeyNotFoundException("Anuncio no encontrado.");
        anuncio.Actualizar(r.ProductoId, r.Titulo, r.Subtitulo, r.Descripcion, r.ImagenUrl, r.PrecioTexto, r.Etiqueta, r.CtaTexto, r.CtaUrl, r.Orden, r.Publicado);
        await uow.SaveChangesAsync(ct);
        var inventario = r.ProductoId is null ? null : await uow.Inventario.ObtenerPorProductoAsync(r.ProductoId.Value, ct);
        return anuncio.ToDto(inventario);
    }

    public async Task<bool> Handle(EliminarAnuncioCatalogoCommand r, CancellationToken ct)
    {
        if (!operationKeyValidator.IsValid(r.ClaveOperacion))
            throw new UnauthorizedAccessException("Clave de operacion invalida.");

        var anuncio = await uow.AnunciosCatalogo.ObtenerPorIdAsync(r.Id, ct) ?? throw new KeyNotFoundException("Anuncio no encontrado.");
        uow.AnunciosCatalogo.Eliminar(anuncio);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    private void ValidarClaveOperacion(string? clave)
    {
        if (!operationKeyValidator.IsValid(clave))
            throw new UnauthorizedAccessException("Clave de operacion invalida.");
    }

    private async Task ValidarProductoAsync(Guid? productoId, CancellationToken ct)
    {
        if (productoId is null) return;
        if (await uow.Productos.ObtenerPorIdAsync(productoId.Value, ct) is null)
            throw new KeyNotFoundException("Producto no encontrado para el anuncio.");
    }

    private static void ValidarAnuncio(string titulo, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new InvalidOperationException("El titulo del anuncio es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion)) throw new InvalidOperationException("La descripcion del anuncio es obligatoria.");
    }
}

public sealed class DocumentoHandlers(IUnitOfWork uow, ICurrentUserService currentUser, IDocumentoFactory factory) :
    IRequestHandler<GenerarDocumentoVentaCommand, DocumentoDto>,
    IRequestHandler<EnviarDocumentoVentaCommand, bool>
{
    public async Task<DocumentoDto> Handle(GenerarDocumentoVentaCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        var usuario = await uow.Usuarios.ObtenerPorIdAsync(usuarioId, ct);
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        var documento = await factory.Crear(r.Tipo).GenerarDocumentoVentaAsync(venta, usuarioId, usuario?.Nombre ?? "Usuario Sicomoro", ct);
        await uow.AgregarAsync(documento, ct);
        await uow.SaveChangesAsync(ct);
        return documento.ToDto();
    }

    public async Task<bool> Handle(EnviarDocumentoVentaCommand r, CancellationToken ct)
    {
        var usuarioId = currentUser.UserId ?? Guid.Empty;
        var usuario = usuarioId == Guid.Empty ? null : await uow.Usuarios.ObtenerPorIdAsync(usuarioId, ct);
        var venta = await uow.Ventas.ObtenerConDetallesAsync(r.VentaId, ct) ?? throw new KeyNotFoundException("Venta no encontrada.");
        var documento = await factory.Crear(r.Tipo).GenerarDocumentoVentaAsync(venta, usuarioId, usuario?.Nombre ?? "Usuario Sicomoro", ct);
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
    public static CompraDto ToDto(this Compra x) => new(
        x.Id,
        x.ProveedorId,
        x.Origen,
        x.Estado,
        x.FechaCompra,
        x.FechaEstimadaLlegada,
        x.Detalles.Sum(d => d.Cantidad * d.PrecioCompra),
        x.CostoTransporte,
        x.OtrosCostos,
        x.Observaciones,
        x.Detalles.Select(d => new CompraDetalleDto(d.ProductoMaderaId, d.Cantidad, d.PrecioCompra)).ToList());
    public static VentaDto ToDto(this Venta x) => new(
        x.Id,
        x.ClienteId,
        x.Fecha,
        x.Estado,
        x.MetodoPago,
        x.FechaVencimiento,
        x.Observaciones,
        x.Total,
        x.MontoPagado,
        x.SaldoPendiente,
        x.Detalles.Select(d => new VentaDetalleDto(d.ProductoMaderaId, d.Cantidad, d.PrecioUnitario, d.Descuento)).ToList());
    public static CobroDto ToDto(this Cobro x) => new(x.Id, x.VentaId, x.ClienteId, x.MontoTotal, x.SaldoPendiente, x.Estado, x.FechaVencimiento);
    public static DocumentoDto ToDto(this DocumentoVenta x) => new(x.Id, x.VentaId, x.Tipo, x.Numero, x.RutaArchivo, x.FechaGeneracion);
    public static CajaMovimientoDto ToDto(this CajaMovimiento x) => new(x.Id, x.Fecha, x.Tipo, x.Monto, x.Concepto, x.UsuarioId, x.VentaId, x.PagoId, x.CompraId);
    public static NotificacionDto ToDto(this Notificacion x) => new(x.Id, x.Tipo, x.Titulo, x.Mensaje, x.UsuarioId, x.Leida, x.CreadoEn);
    public static AuditoriaDto ToDto(this Auditoria x) => new(x.Id, x.UsuarioId, x.FechaHora, x.Accion, x.Entidad, x.EntidadId, x.DatosAntes, x.DatosDespues);
    public static UsuarioDto ToDto(this Usuario x) => new(x.Id, x.Nombre, x.Email, x.Rol, x.Estado, x.CiNit, x.Telefono, x.Direccion, x.Cargo, x.Notas);
    public static AnuncioCatalogoDto ToDto(this AnuncioCatalogo x, Inventario? inventario) => new(x.Id, x.ProductoMaderaId, x.ProductoMadera?.NombreComercial, x.ProductoMadera?.TipoMadera, x.ProductoMadera?.UnidadMedida.ToString(), inventario?.StockActual, x.Titulo, x.Subtitulo, x.Descripcion, x.ImagenUrl, x.PrecioTexto, x.Etiqueta, x.CtaTexto, x.CtaUrl, x.Orden, x.Publicado);
}
