using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sicomoro.Domain.Common;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;
using Sicomoro.Domain.Events;
using Sicomoro.Domain.Interfaces;
using Sicomoro.Infrastructure.Persistence;

namespace Sicomoro.Infrastructure.Repositories;

public class Repository<T>(SicomoroDbContext db) : IRepository<T> where T : EntidadBase
{
    protected readonly SicomoroDbContext Db = db;
    protected readonly DbSet<T> Set = db.Set<T>();

    public virtual Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Set.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public virtual Task<List<T>> ListarAsync(CancellationToken cancellationToken = default) => Set.AsNoTracking().ToListAsync(cancellationToken);
    public virtual Task AgregarAsync(T entity, CancellationToken cancellationToken = default) => Set.AddAsync(entity, cancellationToken).AsTask();
    public virtual void Actualizar(T entity) => Set.Update(entity);
}

public sealed class ClienteRepository(SicomoroDbContext db) : Repository<Cliente>(db), IClienteRepository
{
    public Task<List<Cliente>> BuscarAsync(string? texto, CancellationToken cancellationToken = default)
    {
        var query = Db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(texto))
            query = query.Where(x => x.NombreRazonSocial.Contains(texto) || (x.CiNit != null && x.CiNit.Contains(texto)));
        return query.AsNoTracking().OrderBy(x => x.NombreRazonSocial).ToListAsync(cancellationToken);
    }

    public Task<decimal> ObtenerDeudaTotalAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
        Db.Cobros.Where(x => x.ClienteId == clienteId && x.Estado != EstadoCobro.Pagado).SumAsync(x => x.SaldoPendiente, cancellationToken);

    public async Task<bool> TieneHistorialAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
        await Db.Ventas.AnyAsync(x => x.ClienteId == clienteId, cancellationToken)
        || await Db.Cobros.AnyAsync(x => x.ClienteId == clienteId, cancellationToken);

    public void Eliminar(Cliente cliente) => Db.Clientes.Remove(cliente);
}

public sealed class ProveedorRepository(SicomoroDbContext db) : Repository<Proveedor>(db), IProveedorRepository
{
    public Task<bool> TieneHistorialAsync(Guid proveedorId, CancellationToken cancellationToken = default) =>
        Db.Compras.AnyAsync(x => x.ProveedorId == proveedorId, cancellationToken);

    public void Eliminar(Proveedor proveedor) => Db.Proveedores.Remove(proveedor);
}
public sealed class ProductoRepository(SicomoroDbContext db) : Repository<ProductoMadera>(db), IProductoRepository
{
    public async Task<bool> TieneHistorialAsync(Guid productoId, CancellationToken cancellationToken = default) =>
        await Db.CompraDetalles.AnyAsync(x => x.ProductoMaderaId == productoId, cancellationToken)
        || await Db.VentaDetalles.AnyAsync(x => x.ProductoMaderaId == productoId, cancellationToken)
        || await Db.MovimientosInventario.AnyAsync(x => x.ProductoMaderaId == productoId, cancellationToken);

    public async Task EliminarDefinitivoAsync(ProductoMadera producto, CancellationToken cancellationToken = default)
    {
        var inventario = await Db.Inventario.FirstOrDefaultAsync(x => x.ProductoMaderaId == producto.Id, cancellationToken);
        if (inventario is not null)
        {
            Db.Inventario.Remove(inventario);
        }

        Db.ProductosMadera.Remove(producto);
    }
}
public sealed class TransporteRepository(SicomoroDbContext db) : Repository<Transporte>(db), ITransporteRepository
{
    public override Task<List<Transporte>> ListarAsync(CancellationToken cancellationToken = default) =>
        Db.Transportes.AsNoTracking().OrderByDescending(x => x.FechaSalida ?? x.CreadoEn).ToListAsync(cancellationToken);
}

public sealed class InventarioRepository(SicomoroDbContext db) : Repository<Inventario>(db), IInventarioRepository
{
    public Task<Inventario?> ObtenerPorProductoAsync(Guid productoId, CancellationToken cancellationToken = default) =>
        Db.Inventario.Include(x => x.ProductoMadera).FirstOrDefaultAsync(x => x.ProductoMaderaId == productoId, cancellationToken);

    public Task<List<Inventario>> ObtenerBajoStockAsync(CancellationToken cancellationToken = default) =>
        Db.Inventario.Include(x => x.ProductoMadera).Where(x => x.ProductoMadera != null && x.StockActual <= x.ProductoMadera.StockMinimo).AsNoTracking().ToListAsync(cancellationToken);

    public Task<List<MovimientoInventario>> ListarMovimientosAsync(CancellationToken cancellationToken = default) =>
        Db.MovimientosInventario.AsNoTracking().OrderByDescending(x => x.Fecha).ToListAsync(cancellationToken);

    public Task AgregarMovimientoAsync(MovimientoInventario movimiento, CancellationToken cancellationToken = default) =>
        Db.MovimientosInventario.AddAsync(movimiento, cancellationToken).AsTask();
}

public sealed class CompraRepository(SicomoroDbContext db) : Repository<Compra>(db), ICompraRepository
{
    public override Task<List<Compra>> ListarAsync(CancellationToken cancellationToken = default) =>
        Db.Compras.Include(x => x.Detalles).AsNoTracking().OrderByDescending(x => x.FechaCompra).ToListAsync(cancellationToken);

    public Task<Compra?> ObtenerConDetallesAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.Compras.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

public sealed class VentaRepository(SicomoroDbContext db) : Repository<Venta>(db), IVentaRepository
{
    public override Task<List<Venta>> ListarAsync(CancellationToken cancellationToken = default) =>
        Db.Ventas.Include(x => x.Detalles).AsNoTracking().OrderByDescending(x => x.Fecha).ToListAsync(cancellationToken);

    public Task<Venta?> ObtenerConDetallesAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.Ventas
            .Include(x => x.Detalles)
            .ThenInclude(x => x.ProductoMadera)
            .Include(x => x.Cliente)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

public sealed class CobroRepository(SicomoroDbContext db) : Repository<Cobro>(db), ICobroRepository
{
    public Task<Cobro?> ObtenerPorVentaAsync(Guid ventaId, CancellationToken cancellationToken = default) =>
        Db.Cobros.Include(x => x.Pagos).FirstOrDefaultAsync(x => x.VentaId == ventaId, cancellationToken);

    public Task<List<Cobro>> ObtenerDeudasAsync(CancellationToken cancellationToken = default) =>
        Db.Cobros.Include(x => x.Pagos).Where(x => x.Estado != EstadoCobro.Pagado).AsNoTracking().ToListAsync(cancellationToken);

    public Task<List<Cobro>> ObtenerPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
        Db.Cobros.Include(x => x.Pagos).Where(x => x.ClienteId == clienteId).AsNoTracking().ToListAsync(cancellationToken);
}

public sealed class UsuarioRepository(SicomoroDbContext db) : Repository<Usuario>(db), IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Db.Users.FirstOrDefaultAsync(x => x.Email == email.ToLowerInvariant(), cancellationToken);

    public Task<int> ContarAdministradoresAsync(CancellationToken cancellationToken = default) =>
        Db.Users.CountAsync(x => x.Rol == RolSistema.Administrador, cancellationToken);

    public void Eliminar(Usuario usuario) => Db.Users.Remove(usuario);
}

public sealed class CajaRepository(SicomoroDbContext db) : Repository<CajaMovimiento>(db), ICajaRepository
{
    public Task<List<CajaMovimiento>> ListarPorRangoAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default) =>
        Db.CajaMovimientos.AsNoTracking().Where(x => x.Fecha >= desde && x.Fecha <= hasta).ToListAsync(cancellationToken);
}

public sealed class NotificacionRepository(SicomoroDbContext db) : Repository<Notificacion>(db), INotificacionRepository
{
    public Task<List<Notificacion>> ListarNoLeidasAsync(CancellationToken cancellationToken = default) =>
        Db.Notificaciones.AsNoTracking().Where(x => !x.Leida).OrderByDescending(x => x.CreadoEn).ToListAsync(cancellationToken);
}

public sealed class AuditoriaRepository(SicomoroDbContext db) : Repository<Auditoria>(db), IAuditoriaRepository
{
    public Task<List<Auditoria>> ListarRecienteAsync(int take, CancellationToken cancellationToken = default) =>
        Db.Auditoria.AsNoTracking().OrderByDescending(x => x.FechaHora).Take(take).ToListAsync(cancellationToken);
}

public sealed class AnuncioCatalogoRepository(SicomoroDbContext db) : Repository<AnuncioCatalogo>(db), IAnuncioCatalogoRepository
{
    public Task<List<AnuncioCatalogo>> ListarGestionAsync(CancellationToken cancellationToken = default) =>
        Db.AnunciosCatalogo.Include(x => x.ProductoMadera).AsNoTracking().OrderBy(x => x.Orden).ThenBy(x => x.Titulo).ToListAsync(cancellationToken);

    public Task<List<AnuncioCatalogo>> ListarPublicadosAsync(CancellationToken cancellationToken = default) =>
        Db.AnunciosCatalogo.Include(x => x.ProductoMadera).AsNoTracking().Where(x => x.Publicado).OrderBy(x => x.Orden).ThenBy(x => x.Titulo).ToListAsync(cancellationToken);

    public Task<AnuncioCatalogo?> ObtenerConProductoAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.AnunciosCatalogo.Include(x => x.ProductoMadera).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Eliminar(AnuncioCatalogo anuncio) => Db.AnunciosCatalogo.Remove(anuncio);
}

public sealed class UnitOfWork(SicomoroDbContext db) : IUnitOfWork
{
    public IClienteRepository Clientes { get; } = new ClienteRepository(db);
    public IProveedorRepository Proveedores { get; } = new ProveedorRepository(db);
    public IProductoRepository Productos { get; } = new ProductoRepository(db);
    public ITransporteRepository Transportes { get; } = new TransporteRepository(db);
    public IInventarioRepository Inventario { get; } = new InventarioRepository(db);
    public ICompraRepository Compras { get; } = new CompraRepository(db);
    public IVentaRepository Ventas { get; } = new VentaRepository(db);
    public ICobroRepository Cobros { get; } = new CobroRepository(db);
    public ICajaRepository Caja { get; } = new CajaRepository(db);
    public IUsuarioRepository Usuarios { get; } = new UsuarioRepository(db);
    public INotificacionRepository Notificaciones { get; } = new NotificacionRepository(db);
    public IAuditoriaRepository Auditoria { get; } = new AuditoriaRepository(db);
    public IAnuncioCatalogoRepository AnunciosCatalogo { get; } = new AnuncioCatalogoRepository(db);

    public Task AgregarAsync<T>(T entity, CancellationToken cancellationToken = default) where T : EntidadBase =>
        db.Set<T>().AddAsync(entity, cancellationToken).AsTask();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DispatchDomainEvents();
        return await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfAppTransaction(await db.Database.BeginTransactionAsync(cancellationToken));

    private void DispatchDomainEvents()
    {
        var entities = db.ChangeTracker.Entries<EntidadBase>().Select(x => x.Entity).Where(x => x.DomainEvents.Count > 0).ToList();
        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                switch (domainEvent)
                {
                    case StockBajoEvent e:
                        db.Notificaciones.Add(new Notificacion(TipoNotificacion.BajoStock, "Stock bajo", $"Producto {e.ProductoId} con stock {e.StockActual}."));
                        break;
                    case PagoRegistradoEvent e:
                        db.Notificaciones.Add(new Notificacion(TipoNotificacion.PagoRecibido, "Pago recibido", $"Pago {e.PagoId} por {e.Monto:N2}."));
                        break;
                    case CompraRecibidaEvent e:
                        db.Notificaciones.Add(new Notificacion(TipoNotificacion.PagoRecibido, "Compra recibida", $"Compra {e.CompraId} recibida."));
                        break;
                    case VentaAnuladaEvent e:
                        db.Notificaciones.Add(new Notificacion(TipoNotificacion.VentaAnulada, "Venta anulada", e.Motivo));
                        break;
                }
            }
            entity.ClearDomainEvents();
        }
    }
}

public sealed class EfAppTransaction(IDbContextTransaction transaction) : IAppTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
    public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
