using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;
using Sicomoro.Domain.Interfaces;

namespace Sicomoro.Application.Validators;

public abstract class VentaValidationHandler
{
    private VentaValidationHandler? _next;
    public VentaValidationHandler SetNext(VentaValidationHandler next) { _next = next; return next; }

    public async Task ValidateAsync(Venta venta, RolSistema? rol, CancellationToken cancellationToken)
    {
        await ValidateCoreAsync(venta, rol, cancellationToken);
        if (_next is not null) await _next.ValidateAsync(venta, rol, cancellationToken);
    }

    protected abstract Task ValidateCoreAsync(Venta venta, RolSistema? rol, CancellationToken cancellationToken);
}

public sealed class ValidarClienteActivoHandler(IUnitOfWork uow) : VentaValidationHandler
{
    protected override async Task ValidateCoreAsync(Venta venta, RolSistema? rol, CancellationToken ct)
    {
        var cliente = await uow.Clientes.ObtenerPorIdAsync(venta.ClienteId, ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        if (cliente.Estado != EstadoRegistro.Activo) throw new InvalidOperationException("El cliente esta inactivo.");
    }
}

public sealed class ValidarStockDisponibleHandler(IUnitOfWork uow) : VentaValidationHandler
{
    protected override async Task ValidateCoreAsync(Venta venta, RolSistema? rol, CancellationToken ct)
    {
        foreach (var detalle in venta.Detalles)
        {
            var inventario = await uow.Inventario.ObtenerPorProductoAsync(detalle.ProductoMaderaId, ct);
            if (inventario is null || inventario.StockActual < detalle.Cantidad)
                throw new InvalidOperationException("No se puede vender mas stock del disponible.");
        }
    }
}

public sealed class ValidarPrecioValidoHandler : VentaValidationHandler
{
    protected override Task ValidateCoreAsync(Venta venta, RolSistema? rol, CancellationToken ct)
    {
        if (venta.Detalles.Any(x => x.PrecioUnitario <= 0 || x.Subtotal < 0))
            throw new InvalidOperationException("La venta tiene precios invalidos.");
        return Task.CompletedTask;
    }
}

public sealed class ValidarCreditoClienteHandler(IUnitOfWork uow) : VentaValidationHandler
{
    protected override async Task ValidateCoreAsync(Venta venta, RolSistema? rol, CancellationToken ct)
    {
        var deuda = await uow.Clientes.ObtenerDeudaTotalAsync(venta.ClienteId, ct);
        if (deuda > 50000 && rol != RolSistema.Gerente && rol != RolSistema.Administrador)
            throw new InvalidOperationException("El cliente supera el limite de credito para este rol.");
    }
}

public sealed class ValidarPermisosUsuarioHandler : VentaValidationHandler
{
    protected override Task ValidateCoreAsync(Venta venta, RolSistema? rol, CancellationToken ct)
    {
        if (rol is RolSistema.SoloLectura or null) throw new UnauthorizedAccessException("El usuario no tiene permiso para confirmar ventas.");
        return Task.CompletedTask;
    }
}

public sealed class VentaValidationChain
{
    private readonly VentaValidationHandler _first;

    public VentaValidationChain(ValidarClienteActivoHandler cliente, ValidarStockDisponibleHandler stock, ValidarPrecioValidoHandler precio, ValidarCreditoClienteHandler credito, ValidarPermisosUsuarioHandler permisos)
    {
        _first = cliente;
        cliente.SetNext(stock).SetNext(precio).SetNext(credito).SetNext(permisos);
    }

    public Task ValidarAsync(Venta venta, RolSistema? rol, CancellationToken cancellationToken) => _first.ValidateAsync(venta, rol, cancellationToken);
}

