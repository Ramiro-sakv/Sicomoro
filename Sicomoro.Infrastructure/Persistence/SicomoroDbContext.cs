using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sicomoro.Application.Interfaces;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;
using Sicomoro.Infrastructure.ExternalServices;

namespace Sicomoro.Infrastructure.Persistence;

public sealed class SicomoroDbContext(DbContextOptions<SicomoroDbContext> options, ICurrentUserService? currentUser = null) : DbContext(options)
{
    public DbSet<Usuario> Users => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permissions => Set<Permiso>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<ProductoMadera> ProductosMadera => Set<ProductoMadera>();
    public DbSet<Inventario> Inventario => Set<Inventario>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    public DbSet<Transporte> Transportes => Set<Transporte>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<Cobro> Cobros => Set<Cobro>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<CajaMovimiento> CajaMovimientos => Set<CajaMovimiento>();
    public DbSet<DocumentoVenta> DocumentosVenta => Set<DocumentoVenta>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Auditoria> Auditoria => Set<Auditoria>();
    public DbSet<AnuncioCatalogo> AnunciosCatalogo => Set<AnuncioCatalogo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SicomoroDbContext).Assembly);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (!string.IsNullOrWhiteSpace(table)) entity.SetTableName(table);
        }

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        modelBuilder.Entity<Usuario>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Inventario>().HasIndex(x => x.ProductoMaderaId).IsUnique();
        modelBuilder.Entity<Compra>().HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Venta>().HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Cobro>().HasMany(x => x.Pagos).WithOne().HasForeignKey(x => x.CobroId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AnuncioCatalogo>().HasOne(x => x.ProductoMadera).WithMany().HasForeignKey(x => x.ProductoMaderaId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AnuncioCatalogo>().HasIndex(x => new { x.Publicado, x.Orden });

        Seed(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddAuditoria()
    {
        var entradas = ChangeTracker.Entries()
            .Where(e => e.Entity is not Sicomoro.Domain.Entities.Auditoria && e.State is (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            .ToList();

        foreach (var entry in entradas)
        {
            var idValue = entry.Properties.FirstOrDefault(x => x.Metadata.Name == "Id")?.CurrentValue;
            var id = idValue is Guid guid ? guid : (Guid?)null;
            var antes = entry.State == EntityState.Added ? null : JsonSerializer.Serialize(entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p]));
            var despues = entry.State == EntityState.Deleted ? null : JsonSerializer.Serialize(entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p]));
            Auditoria.Add(new Auditoria(currentUser?.UserId, entry.State.ToString(), entry.Entity.GetType().Name, id, antes, despues));
        }
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Rol>().HasData(
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = RolSistema.Administrador, Nombre = "Administrador" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = RolSistema.Vendedor, Nombre = "Vendedor" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = RolSistema.Inventario, Nombre = "Encargado de inventario" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = RolSistema.Cobrador, Nombre = "Cobrador" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = RolSistema.Gerente, Nombre = "Dueno / gerente" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = RolSistema.SoloLectura, Nombre = "Solo lectura" });
        modelBuilder.Entity<Permiso>().HasData(
            new { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = "clientes.leer", Descripcion = "Ver clientes" },
            new { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = "ventas.confirmar", Descripcion = "Confirmar ventas" },
            new { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = "inventario.ajustar", Descripcion = "Ajustar inventario" },
            new { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, Codigo = "reportes.sensibles", Descripcion = "Ver reportes sensibles" });

        modelBuilder.Entity<Usuario>().HasData(new
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            CreadoEn = seedDate,
            ActualizadoEn = (DateTime?)null,
            Nombre = "Administrador Sicomoro",
            Email = "admin@sicomoro.local",
            PasswordHash = PasswordHasher.HashDeterministic("Admin123*"),
            Rol = RolSistema.Administrador,
            Estado = EstadoRegistro.Activo,
            CiNit = (string?)null,
            Telefono = (string?)null,
            Direccion = (string?)null,
            Cargo = (string?)null,
            Notas = (string?)null
        });
        modelBuilder.Entity<ProductoMadera>().HasData(
            new { Id = Guid.Parse("40000000-0000-0000-0000-000000000001"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, NombreComercial = "Tajibo 2x4", TipoMadera = "Tajibo", UnidadMedida = UnidadMedida.Pieza, Largo = 2m, Ancho = 4m, Espesor = 0m, Calidad = "A", PrecioCompra = 35m, PrecioVentaSugerido = 55m, Estado = EstadoRegistro.Activo, StockMinimo = 10m, Observaciones = "Producto inicial" },
            new { Id = Guid.Parse("40000000-0000-0000-0000-000000000002"), CreadoEn = seedDate, ActualizadoEn = (DateTime?)null, NombreComercial = "Cedro tabla", TipoMadera = "Cedro", UnidadMedida = UnidadMedida.Tabla, Largo = 2.5m, Ancho = 0.3m, Espesor = 0.05m, Calidad = "A", PrecioCompra = 45m, PrecioVentaSugerido = 70m, Estado = EstadoRegistro.Activo, StockMinimo = 8m, Observaciones = "Producto inicial" });
    }
}
