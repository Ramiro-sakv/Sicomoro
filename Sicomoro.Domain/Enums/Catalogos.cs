namespace Sicomoro.Domain.Enums;

public enum EstadoRegistro { Activo = 1, Inactivo = 2 }
public enum RolSistema { Administrador = 1, Vendedor = 2, Inventario = 3, Cobrador = 4, Gerente = 5, SoloLectura = 6 }
public enum UnidadMedida { Pieza = 1, Tabla = 2, Tablon = 3, Viga = 4, MetroCubico = 5, PieTablar = 6, Otra = 99 }
public enum TipoMovimientoInventario { Entrada = 1, SalidaVenta = 2, AjusteManual = 3, Perdida = 4, Devolucion = 5, EntradaCompra = 6, ReversionVenta = 7 }
public enum EstadoCompra { Pendiente = 1, EnTransito = 2, Recibida = 3, Cancelada = 4 }
public enum EstadoTransporte { Programado = 1, EnRuta = 2, Llegado = 3, Cancelado = 4 }
public enum EstadoVenta { Pendiente = 1, Pagada = 2, ParcialmentePagada = 3, Anulada = 4 }
public enum EstadoCobro { Pendiente = 1, Parcial = 2, Pagado = 3, Vencido = 4 }
public enum MetodoPago { Efectivo = 1, Transferencia = 2, QR = 3, Tarjeta = 4, Credito = 5, Mixto = 6 }
public enum TipoCajaMovimiento { Ingreso = 1, Egreso = 2 }
public enum TipoDocumentoVenta { ComprobanteVenta = 1, ReciboPago = 2, NotaEntrega = 3, DocumentoFiscalFuturo = 99 }
public enum TipoNotificacion { BajoStock = 1, DeudaVencida = 2, CompraRetrasada = 3, VentaAnulada = 4, PagoRecibido = 5, ProductoSinPrecio = 6 }

