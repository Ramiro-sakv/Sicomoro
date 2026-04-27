using Sicomoro.Domain.Common;
using Sicomoro.Domain.Enums;
using Sicomoro.Domain.Events;

namespace Sicomoro.Domain.Entities;

public sealed class Usuario : EntidadBase
{
    private Usuario() { }
    public Usuario(string nombre, string email, string passwordHash, RolSistema rol, string? ciNit = null, string? telefono = null, string? direccion = null, string? cargo = null, string? notas = null)
    {
        Nombre = nombre.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Rol = rol;
        CiNit = ciNit;
        Telefono = telefono;
        Direccion = direccion;
        Cargo = cargo;
        Notas = notas;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public RolSistema Rol { get; private set; }
    public EstadoRegistro Estado { get; private set; } = EstadoRegistro.Activo;
    public string? CiNit { get; private set; }
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
    public string? Cargo { get; private set; }
    public string? Notas { get; private set; }

    public void ActualizarPerfil(string nombre, string email, string? ciNit, string? telefono, string? direccion, string? cargo, string? notas)
    {
        Nombre = nombre.Trim();
        Email = email.Trim().ToLowerInvariant();
        CiNit = ciNit;
        Telefono = telefono;
        Direccion = direccion;
        Cargo = cargo;
        Notas = notas;
        MarcarActualizado();
    }

    public void CambiarPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MarcarActualizado();
    }
}

public sealed class Rol : EntidadBase
{
    private Rol() { }
    public Rol(RolSistema codigo, string nombre) { Codigo = codigo; Nombre = nombre; }
    public RolSistema Codigo { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
}

public sealed class Permiso : EntidadBase
{
    private Permiso() { }
    public Permiso(string codigo, string descripcion) { Codigo = codigo; Descripcion = descripcion; }
    public string Codigo { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
}

public sealed class Cliente : EntidadBase
{
    private Cliente() { }
    public Cliente(string nombre, string? ciNit, string? telefono, string? direccion, string? ciudad, string? notas)
    {
        NombreRazonSocial = nombre.Trim();
        CiNit = ciNit;
        Telefono = telefono;
        Direccion = direccion;
        Ciudad = ciudad;
        Notas = notas;
    }

    public string NombreRazonSocial { get; private set; } = string.Empty;
    public string? CiNit { get; private set; }
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
    public string? Ciudad { get; private set; }
    public string? Notas { get; private set; }
    public EstadoRegistro Estado { get; private set; } = EstadoRegistro.Activo;
    public List<Venta> Ventas { get; private set; } = [];
    public List<Cobro> Cobros { get; private set; } = [];

    public void Actualizar(string nombre, string? ciNit, string? telefono, string? direccion, string? ciudad, string? notas, EstadoRegistro estado)
    {
        NombreRazonSocial = nombre.Trim();
        CiNit = ciNit;
        Telefono = telefono;
        Direccion = direccion;
        Ciudad = ciudad;
        Notas = notas;
        Estado = estado;
        MarcarActualizado();
    }
}

public sealed class Proveedor : EntidadBase
{
    private Proveedor() { }
    public Proveedor(string nombre, string origen, string? telefono, string? direccion, string? tipoMadera, string? notas)
    {
        Nombre = nombre.Trim();
        LugarOrigen = origen.Trim();
        Telefono = telefono;
        Direccion = direccion;
        TipoMadera = tipoMadera;
        Notas = notas;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string LugarOrigen { get; private set; } = string.Empty;
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
    public string? TipoMadera { get; private set; }
    public string? Notas { get; private set; }
}

public sealed class ProductoMadera : EntidadBase
{
    private ProductoMadera() { }
    public ProductoMadera(string nombre, string tipoMadera, UnidadMedida unidad, decimal largo, decimal ancho, decimal espesor, string? calidad, decimal precioCompra, decimal precioVenta, decimal stockMinimo, string? observaciones)
    {
        NombreComercial = nombre.Trim();
        TipoMadera = tipoMadera.Trim();
        UnidadMedida = unidad;
        Largo = largo;
        Ancho = ancho;
        Espesor = espesor;
        Calidad = calidad;
        PrecioCompra = precioCompra;
        PrecioVentaSugerido = precioVenta;
        StockMinimo = stockMinimo;
        Observaciones = observaciones;
    }

    public string NombreComercial { get; private set; } = string.Empty;
    public string TipoMadera { get; private set; } = string.Empty;
    public UnidadMedida UnidadMedida { get; private set; }
    public decimal Largo { get; private set; }
    public decimal Ancho { get; private set; }
    public decimal Espesor { get; private set; }
    public string? Calidad { get; private set; }
    public decimal PrecioCompra { get; private set; }
    public decimal PrecioVentaSugerido { get; private set; }
    public EstadoRegistro Estado { get; private set; } = EstadoRegistro.Activo;
    public decimal StockMinimo { get; private set; }
    public string? Observaciones { get; private set; }

    public void ActualizarPrecios(decimal precioCompra, decimal precioVenta)
    {
        PrecioCompra = precioCompra;
        PrecioVentaSugerido = precioVenta;
        MarcarActualizado();
    }

    public void Actualizar(string nombre, string tipoMadera, UnidadMedida unidad, decimal largo, decimal ancho, decimal espesor, string? calidad, decimal precioCompra, decimal precioVenta, decimal stockMinimo, EstadoRegistro estado, string? observaciones)
    {
        NombreComercial = nombre.Trim();
        TipoMadera = tipoMadera.Trim();
        UnidadMedida = unidad;
        Largo = largo;
        Ancho = ancho;
        Espesor = espesor;
        Calidad = calidad;
        PrecioCompra = precioCompra;
        PrecioVentaSugerido = precioVenta;
        StockMinimo = stockMinimo;
        Estado = estado;
        Observaciones = observaciones;
        MarcarActualizado();
    }

    public void Inactivar()
    {
        Estado = EstadoRegistro.Inactivo;
        MarcarActualizado();
    }
}

public sealed class Inventario : EntidadBase
{
    private Inventario() { }
    public Inventario(Guid productoId, string? ubicacion)
    {
        ProductoMaderaId = productoId;
        UbicacionInterna = ubicacion;
    }

    public Guid ProductoMaderaId { get; private set; }
    public ProductoMadera? ProductoMadera { get; private set; }
    public decimal StockActual { get; private set; }
    public string? UbicacionInterna { get; private set; }

    public void Incrementar(decimal cantidad)
    {
        if (cantidad <= 0) throw new InvalidOperationException("La cantidad debe ser mayor a cero.");
        StockActual += cantidad;
        MarcarActualizado();
    }

    public void Descontar(decimal cantidad)
    {
        if (cantidad <= 0) throw new InvalidOperationException("La cantidad debe ser mayor a cero.");
        if (StockActual < cantidad) throw new InvalidOperationException("No se puede vender mas stock del disponible.");
        StockActual -= cantidad;
        MarcarActualizado();
        if (ProductoMadera is not null && StockActual <= ProductoMadera.StockMinimo)
            AddDomainEvent(new StockBajoEvent(ProductoMaderaId, StockActual, ProductoMadera.StockMinimo, DateTime.UtcNow));
    }

    public void Ajustar(decimal nuevoStock, string? ubicacion)
    {
        if (nuevoStock < 0) throw new InvalidOperationException("El stock no puede ser negativo.");
        StockActual = nuevoStock;
        UbicacionInterna = ubicacion;
        MarcarActualizado();
    }
}

public sealed class MovimientoInventario : EntidadBase
{
    private MovimientoInventario() { }
    public MovimientoInventario(Guid productoId, Guid usuarioId, TipoMovimientoInventario tipo, decimal cantidad, decimal costoUnitario, string motivo, Guid? ventaId = null, Guid? compraId = null)
    {
        ProductoMaderaId = productoId;
        UsuarioId = usuarioId;
        Tipo = tipo;
        Cantidad = cantidad;
        CostoUnitario = costoUnitario;
        Motivo = motivo;
        VentaId = ventaId;
        CompraId = compraId;
    }

    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; private set; }
    public Guid ProductoMaderaId { get; private set; }
    public TipoMovimientoInventario Tipo { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public Guid? VentaId { get; private set; }
    public Guid? CompraId { get; private set; }
}

public sealed class Compra : EntidadBase
{
    private Compra() { }
    public Compra(Guid proveedorId, string origen, DateTime fechaCompra, DateTime? fechaEstimadaLlegada, decimal costoTransporte, decimal otrosCostos, string? observaciones)
    {
        ProveedorId = proveedorId;
        Origen = origen.Trim();
        FechaCompra = fechaCompra;
        FechaEstimadaLlegada = fechaEstimadaLlegada;
        CostoTransporte = costoTransporte;
        OtrosCostos = otrosCostos;
        Observaciones = observaciones;
    }

    public Guid ProveedorId { get; private set; }
    public Proveedor? Proveedor { get; private set; }
    public string Origen { get; private set; } = string.Empty;
    public DateTime FechaCompra { get; private set; }
    public DateTime? FechaEstimadaLlegada { get; private set; }
    public decimal CostoTransporte { get; private set; }
    public decimal OtrosCostos { get; private set; }
    public EstadoCompra Estado { get; private set; } = EstadoCompra.Pendiente;
    public string? Observaciones { get; private set; }
    public List<CompraDetalle> Detalles { get; private set; } = [];

    public void AgregarDetalle(Guid productoId, decimal cantidad, decimal precioCompra)
    {
        if (cantidad <= 0 || precioCompra < 0) throw new InvalidOperationException("Detalle de compra invalido.");
        Detalles.Add(new CompraDetalle(Id, productoId, cantidad, precioCompra));
    }

    public void ActualizarPendiente(Guid proveedorId, string origen, DateTime fechaCompra, DateTime? fechaEstimadaLlegada, decimal costoTransporte, decimal otrosCostos, string? observaciones)
    {
        if (Estado != EstadoCompra.Pendiente) throw new InvalidOperationException("Solo se puede editar una compra pendiente.");
        ProveedorId = proveedorId;
        Origen = origen.Trim();
        FechaCompra = fechaCompra;
        FechaEstimadaLlegada = fechaEstimadaLlegada;
        CostoTransporte = costoTransporte;
        OtrosCostos = otrosCostos;
        Observaciones = observaciones;
        MarcarActualizado();
    }

    public void LimpiarDetallesPendiente()
    {
        if (Estado != EstadoCompra.Pendiente) throw new InvalidOperationException("Solo se pueden cambiar detalles de una compra pendiente.");
        Detalles.Clear();
        MarcarActualizado();
    }

    public void MarcarEnTransito()
    {
        if (Estado is EstadoCompra.Recibida or EstadoCompra.Cancelada) throw new InvalidOperationException("La compra no puede cambiar a transito.");
        Estado = EstadoCompra.EnTransito;
        MarcarActualizado();
    }

    public void Recibir()
    {
        if (Estado == EstadoCompra.Recibida) throw new InvalidOperationException("Una compra recibida no debe volver a modificar inventario.");
        if (Estado == EstadoCompra.Cancelada) throw new InvalidOperationException("Una compra cancelada no puede recibirse.");
        Estado = EstadoCompra.Recibida;
        AddDomainEvent(new CompraRecibidaEvent(Id, ProveedorId, DateTime.UtcNow));
        MarcarActualizado();
    }
}

public sealed class CompraDetalle : EntidadBase
{
    private CompraDetalle() { }
    public CompraDetalle(Guid compraId, Guid productoId, decimal cantidad, decimal precioCompra)
    {
        CompraId = compraId;
        ProductoMaderaId = productoId;
        Cantidad = cantidad;
        PrecioCompra = precioCompra;
    }

    public Guid CompraId { get; private set; }
    public Guid ProductoMaderaId { get; private set; }
    public ProductoMadera? ProductoMadera { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal PrecioCompra { get; private set; }
}

public sealed class Transporte : EntidadBase
{
    private Transporte() { }
    public Transporte(string? camion, string? chofer, string? placa, string lugarOrigen, DateTime? fechaSalida, DateTime? fechaLlegada, decimal costoTransporte, EstadoTransporte estado, string? observaciones, Guid? compraId = null)
    {
        Camion = camion;
        Chofer = chofer;
        Placa = placa;
        LugarOrigen = lugarOrigen.Trim();
        FechaSalida = fechaSalida;
        FechaLlegada = fechaLlegada;
        CostoTransporte = costoTransporte;
        Estado = estado;
        Observaciones = observaciones;
        CompraId = compraId;
    }

    public string? Camion { get; private set; }
    public string? Chofer { get; private set; }
    public string? Placa { get; private set; }
    public string LugarOrigen { get; private set; } = string.Empty;
    public DateTime? FechaSalida { get; private set; }
    public DateTime? FechaLlegada { get; private set; }
    public decimal CostoTransporte { get; private set; }
    public EstadoTransporte Estado { get; private set; } = EstadoTransporte.Programado;
    public string? Observaciones { get; private set; }
    public Guid? CompraId { get; private set; }

    public void ActualizarEstado(EstadoTransporte estado, DateTime? fechaLlegada = null)
    {
        Estado = estado;
        FechaLlegada = fechaLlegada ?? FechaLlegada;
        MarcarActualizado();
    }
}

public sealed class Venta : EntidadBase
{
    private Venta() { }
    public Venta(Guid clienteId, Guid vendedorId, MetodoPago metodoPago, DateTime? vencimiento, string? observaciones)
    {
        ClienteId = clienteId;
        VendedorId = vendedorId;
        MetodoPago = metodoPago;
        Fecha = DateTime.UtcNow;
        FechaVencimiento = vencimiento;
        Observaciones = observaciones;
    }

    public Guid ClienteId { get; private set; }
    public Cliente? Cliente { get; private set; }
    public Guid VendedorId { get; private set; }
    public DateTime Fecha { get; private set; }
    public MetodoPago MetodoPago { get; private set; }
    public EstadoVenta Estado { get; private set; } = EstadoVenta.Pendiente;
    public decimal Total { get; private set; }
    public decimal MontoPagado { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public string? Observaciones { get; private set; }
    public List<VentaDetalle> Detalles { get; private set; } = [];

    public decimal SaldoPendiente => Total - MontoPagado;

    public void AgregarDetalle(Guid productoId, decimal cantidad, decimal precioUnitario, decimal descuento)
    {
        if (cantidad <= 0 || precioUnitario < 0 || descuento < 0) throw new InvalidOperationException("Detalle de venta invalido.");
        Detalles.Add(new VentaDetalle(Id, productoId, cantidad, precioUnitario, descuento));
        RecalcularTotal();
    }

    public void ActualizarPendiente(Guid clienteId, MetodoPago metodoPago, DateTime? vencimiento, string? observaciones)
    {
        if (Estado != EstadoVenta.Pendiente) throw new InvalidOperationException("Solo se puede editar una venta pendiente.");
        ClienteId = clienteId;
        MetodoPago = metodoPago;
        FechaVencimiento = vencimiento;
        Observaciones = observaciones;
        MarcarActualizado();
    }

    public void LimpiarDetallesPendiente()
    {
        if (Estado != EstadoVenta.Pendiente) throw new InvalidOperationException("Solo se pueden cambiar detalles de una venta pendiente.");
        Detalles.Clear();
        RecalcularTotal();
        MarcarActualizado();
    }

    public void Confirmar(decimal montoPagado)
    {
        if (Estado == EstadoVenta.Anulada) throw new InvalidOperationException("Una venta anulada no puede confirmarse.");
        if (Detalles.Count == 0) throw new InvalidOperationException("No se puede confirmar una venta sin detalle.");
        if (montoPagado < 0 || montoPagado > Total) throw new InvalidOperationException("El pago inicial no puede exceder el total.");
        MontoPagado = montoPagado;
        Estado = montoPagado == Total ? EstadoVenta.Pagada : montoPagado > 0 ? EstadoVenta.ParcialmentePagada : EstadoVenta.Pendiente;
        AddDomainEvent(new VentaConfirmadaEvent(Id, ClienteId, Total, DateTime.UtcNow));
        MarcarActualizado();
    }

    public void RegistrarPago(decimal monto)
    {
        if (Estado == EstadoVenta.Anulada) throw new InvalidOperationException("Una venta anulada no permite pagos nuevos.");
        if (monto <= 0 || monto > SaldoPendiente) throw new InvalidOperationException("No se puede registrar pago mayor al saldo pendiente.");
        MontoPagado += monto;
        Estado = SaldoPendiente == 0 ? EstadoVenta.Pagada : EstadoVenta.ParcialmentePagada;
        MarcarActualizado();
    }

    public void Anular(string motivo)
    {
        if (Estado == EstadoVenta.Anulada) return;
        Estado = EstadoVenta.Anulada;
        AddDomainEvent(new VentaAnuladaEvent(Id, motivo, DateTime.UtcNow));
        MarcarActualizado();
    }

    private void RecalcularTotal() => Total = Detalles.Sum(x => x.Subtotal);
}

public sealed class VentaDetalle : EntidadBase
{
    private VentaDetalle() { }
    public VentaDetalle(Guid ventaId, Guid productoId, decimal cantidad, decimal precioUnitario, decimal descuento)
    {
        VentaId = ventaId;
        ProductoMaderaId = productoId;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        Descuento = descuento;
    }

    public Guid VentaId { get; private set; }
    public Guid ProductoMaderaId { get; private set; }
    public ProductoMadera? ProductoMadera { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Descuento { get; private set; }
    public decimal Subtotal => (Cantidad * PrecioUnitario) - Descuento;
}

public sealed class Cobro : EntidadBase
{
    private Cobro() { }
    public Cobro(Guid ventaId, Guid clienteId, decimal montoTotal, DateTime? vencimiento)
    {
        VentaId = ventaId;
        ClienteId = clienteId;
        MontoTotal = montoTotal;
        SaldoPendiente = montoTotal;
        FechaVencimiento = vencimiento;
    }

    public Guid VentaId { get; private set; }
    public Guid ClienteId { get; private set; }
    public decimal MontoTotal { get; private set; }
    public decimal SaldoPendiente { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public EstadoCobro Estado { get; private set; } = EstadoCobro.Pendiente;
    public List<Pago> Pagos { get; private set; } = [];

    public Pago RegistrarPago(decimal monto, MetodoPago metodo, Guid usuarioId, string? referencia)
    {
        if (Estado == EstadoCobro.Pagado) throw new InvalidOperationException("El cobro ya esta pagado.");
        if (monto <= 0 || monto > SaldoPendiente) throw new InvalidOperationException("No se puede registrar pago mayor al saldo pendiente.");
        var pago = new Pago(Id, monto, metodo, usuarioId, referencia);
        Pagos.Add(pago);
        SaldoPendiente -= monto;
        Estado = SaldoPendiente == 0 ? EstadoCobro.Pagado : EstadoCobro.Parcial;
        AddDomainEvent(new PagoRegistradoEvent(pago.Id, Id, monto, DateTime.UtcNow));
        MarcarActualizado();
        return pago;
    }
}

public sealed class Pago : EntidadBase
{
    private Pago() { }
    public Pago(Guid cobroId, decimal monto, MetodoPago metodoPago, Guid usuarioId, string? referencia)
    {
        CobroId = cobroId;
        Monto = monto;
        MetodoPago = metodoPago;
        UsuarioId = usuarioId;
        Referencia = referencia;
    }

    public Guid CobroId { get; private set; }
    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public decimal Monto { get; private set; }
    public MetodoPago MetodoPago { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string? Referencia { get; private set; }
}

public sealed class CajaMovimiento : EntidadBase
{
    private CajaMovimiento() { }
    public CajaMovimiento(TipoCajaMovimiento tipo, decimal monto, string concepto, Guid usuarioId, Guid? ventaId = null, Guid? pagoId = null, Guid? compraId = null)
    {
        Tipo = tipo;
        Monto = monto;
        Concepto = concepto;
        UsuarioId = usuarioId;
        VentaId = ventaId;
        PagoId = pagoId;
        CompraId = compraId;
    }

    public DateTime Fecha { get; private set; } = DateTime.UtcNow;
    public TipoCajaMovimiento Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public string Concepto { get; private set; } = string.Empty;
    public Guid UsuarioId { get; private set; }
    public Guid? VentaId { get; private set; }
    public Guid? PagoId { get; private set; }
    public Guid? CompraId { get; private set; }
}

public sealed class DocumentoVenta : EntidadBase
{
    private DocumentoVenta() { }
    public DocumentoVenta(Guid ventaId, TipoDocumentoVenta tipo, string numero, string rutaArchivo, Guid usuarioId)
    {
        VentaId = ventaId;
        Tipo = tipo;
        Numero = numero;
        RutaArchivo = rutaArchivo;
        UsuarioId = usuarioId;
    }

    public Guid VentaId { get; private set; }
    public TipoDocumentoVenta Tipo { get; private set; }
    public string Numero { get; private set; } = string.Empty;
    public string RutaArchivo { get; private set; } = string.Empty;
    public DateTime FechaGeneracion { get; private set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; private set; }
}

public sealed class Notificacion : EntidadBase
{
    private Notificacion() { }
    public Notificacion(TipoNotificacion tipo, string titulo, string mensaje, Guid? usuarioId = null)
    {
        Tipo = tipo;
        Titulo = titulo;
        Mensaje = mensaje;
        UsuarioId = usuarioId;
    }

    public TipoNotificacion Tipo { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Mensaje { get; private set; } = string.Empty;
    public Guid? UsuarioId { get; private set; }
    public bool Leida { get; private set; }
}

public sealed class Auditoria : EntidadBase
{
    private Auditoria() { }
    public Auditoria(Guid? usuarioId, string accion, string entidad, Guid? entidadId, string? datosAntes, string? datosDespues)
    {
        UsuarioId = usuarioId;
        Accion = accion;
        Entidad = entidad;
        EntidadId = entidadId;
        DatosAntes = datosAntes;
        DatosDespues = datosDespues;
    }

    public Guid? UsuarioId { get; private set; }
    public DateTime FechaHora { get; private set; } = DateTime.UtcNow;
    public string Accion { get; private set; } = string.Empty;
    public string Entidad { get; private set; } = string.Empty;
    public Guid? EntidadId { get; private set; }
    public string? DatosAntes { get; private set; }
    public string? DatosDespues { get; private set; }
}
