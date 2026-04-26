using Sicomoro.Domain.Common;
using Sicomoro.Domain.Entities;

namespace Sicomoro.Domain.Interfaces;

public interface IRepository<T> where T : EntidadBase
{
    Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<T>> ListarAsync(CancellationToken cancellationToken = default);
    Task AgregarAsync(T entity, CancellationToken cancellationToken = default);
    void Actualizar(T entity);
}

public interface IClienteRepository : IRepository<Cliente>
{
    Task<List<Cliente>> BuscarAsync(string? texto, CancellationToken cancellationToken = default);
    Task<decimal> ObtenerDeudaTotalAsync(Guid clienteId, CancellationToken cancellationToken = default);
}

public interface IProveedorRepository : IRepository<Proveedor> { }
public interface IProductoRepository : IRepository<ProductoMadera>
{
    Task<bool> TieneHistorialAsync(Guid productoId, CancellationToken cancellationToken = default);
    Task EliminarDefinitivoAsync(ProductoMadera producto, CancellationToken cancellationToken = default);
}
public interface ITransporteRepository : IRepository<Transporte> { }

public interface IInventarioRepository : IRepository<Inventario>
{
    Task<Inventario?> ObtenerPorProductoAsync(Guid productoId, CancellationToken cancellationToken = default);
    Task<List<Inventario>> ObtenerBajoStockAsync(CancellationToken cancellationToken = default);
    Task<List<MovimientoInventario>> ListarMovimientosAsync(CancellationToken cancellationToken = default);
    Task AgregarMovimientoAsync(MovimientoInventario movimiento, CancellationToken cancellationToken = default);
}

public interface ICompraRepository : IRepository<Compra>
{
    Task<Compra?> ObtenerConDetallesAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IVentaRepository : IRepository<Venta>
{
    Task<Venta?> ObtenerConDetallesAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICobroRepository : IRepository<Cobro>
{
    Task<Cobro?> ObtenerPorVentaAsync(Guid ventaId, CancellationToken cancellationToken = default);
    Task<List<Cobro>> ObtenerDeudasAsync(CancellationToken cancellationToken = default);
    Task<List<Cobro>> ObtenerPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);
}

public interface ICajaRepository : IRepository<CajaMovimiento>
{
    Task<List<CajaMovimiento>> ListarPorRangoAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
}

public interface INotificacionRepository : IRepository<Notificacion>
{
    Task<List<Notificacion>> ListarNoLeidasAsync(CancellationToken cancellationToken = default);
}

public interface IAuditoriaRepository : IRepository<Auditoria>
{
    Task<List<Auditoria>> ListarRecienteAsync(int take, CancellationToken cancellationToken = default);
}

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    IClienteRepository Clientes { get; }
    IProveedorRepository Proveedores { get; }
    IProductoRepository Productos { get; }
    ITransporteRepository Transportes { get; }
    IInventarioRepository Inventario { get; }
    ICompraRepository Compras { get; }
    IVentaRepository Ventas { get; }
    ICobroRepository Cobros { get; }
    ICajaRepository Caja { get; }
    IUsuarioRepository Usuarios { get; }
    INotificacionRepository Notificaciones { get; }
    IAuditoriaRepository Auditoria { get; }
    Task AgregarAsync<T>(T entity, CancellationToken cancellationToken = default) where T : EntidadBase;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
