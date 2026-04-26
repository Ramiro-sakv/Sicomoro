using Sicomoro.Domain.Common;

namespace Sicomoro.Domain.Events;

public sealed record VentaConfirmadaEvent(Guid VentaId, Guid ClienteId, decimal Total, DateTime OcurridoEn) : IDomainEvent;
public sealed record StockBajoEvent(Guid ProductoId, decimal StockActual, decimal StockMinimo, DateTime OcurridoEn) : IDomainEvent;
public sealed record PagoRegistradoEvent(Guid PagoId, Guid CobroId, decimal Monto, DateTime OcurridoEn) : IDomainEvent;
public sealed record CompraRecibidaEvent(Guid CompraId, Guid ProveedorId, DateTime OcurridoEn) : IDomainEvent;
public sealed record VentaAnuladaEvent(Guid VentaId, string Motivo, DateTime OcurridoEn) : IDomainEvent;

