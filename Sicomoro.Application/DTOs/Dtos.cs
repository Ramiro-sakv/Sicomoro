using Sicomoro.Domain.Enums;

namespace Sicomoro.Application.DTOs;

public sealed record ClienteDto(Guid Id, string NombreRazonSocial, string? CiNit, string? Telefono, string? Direccion, string? Ciudad, string? Notas, EstadoRegistro Estado, decimal DeudaTotal);
public sealed record ProveedorDto(Guid Id, string Nombre, string LugarOrigen, string? Telefono, string? Direccion, string? TipoMadera, string? Notas);
public sealed record ProductoDto(Guid Id, string NombreComercial, string TipoMadera, UnidadMedida UnidadMedida, decimal Largo, decimal Ancho, decimal Espesor, string? Calidad, decimal PrecioCompra, decimal PrecioVentaSugerido, decimal StockMinimo, EstadoRegistro Estado);
public sealed record InventarioDto(Guid Id, Guid ProductoId, string Producto, decimal StockActual, decimal StockMinimo, string? UbicacionInterna);
public sealed record MovimientoInventarioDto(Guid Id, DateTime Fecha, Guid ProductoId, TipoMovimientoInventario Tipo, decimal Cantidad, decimal CostoUnitario, string Motivo);
public sealed record TransporteDto(Guid Id, string? Camion, string? Chofer, string? Placa, string LugarOrigen, DateTime? FechaSalida, DateTime? FechaLlegada, decimal CostoTransporte, EstadoTransporte Estado, string? Observaciones, Guid? CompraId);
public sealed record CompraDetalleInput(Guid ProductoId, decimal Cantidad, decimal PrecioCompra);
public sealed record VentaDetalleInput(Guid ProductoId, decimal Cantidad, decimal? PrecioUnitario, decimal Descuento, string PricingStrategy = "normal");
public sealed record CompraDto(Guid Id, Guid ProveedorId, string Origen, EstadoCompra Estado, DateTime FechaCompra, decimal TotalProductos, decimal CostoTransporte, decimal OtrosCostos);
public sealed record VentaDto(Guid Id, Guid ClienteId, DateTime Fecha, EstadoVenta Estado, decimal Total, decimal MontoPagado, decimal SaldoPendiente);
public sealed record CobroDto(Guid Id, Guid VentaId, Guid ClienteId, decimal MontoTotal, decimal SaldoPendiente, EstadoCobro Estado, DateTime? FechaVencimiento);
public sealed record DocumentoDto(Guid Id, Guid VentaId, TipoDocumentoVenta Tipo, string Numero, string RutaArchivo, DateTime FechaGeneracion);
public sealed record CajaMovimientoDto(Guid Id, DateTime Fecha, TipoCajaMovimiento Tipo, decimal Monto, string Concepto, Guid UsuarioId, Guid? VentaId, Guid? PagoId, Guid? CompraId);
public sealed record NotificacionDto(Guid Id, TipoNotificacion Tipo, string Titulo, string Mensaje, Guid? UsuarioId, bool Leida, DateTime CreadoEn);
public sealed record AuditoriaDto(Guid Id, Guid? UsuarioId, DateTime FechaHora, string Accion, string Entidad, Guid? EntidadId, string? DatosAntes, string? DatosDespues);
public sealed record UsuarioDto(Guid Id, string Nombre, string Email, RolSistema Rol, EstadoRegistro Estado);
public sealed record ReporteVentasDto(DateTime Desde, DateTime Hasta, int CantidadVentas, decimal TotalVentas, decimal TotalPagado, decimal SaldoPendiente);
public sealed record ReporteCajaDto(DateTime Desde, DateTime Hasta, decimal Ingresos, decimal Egresos, decimal Saldo);
