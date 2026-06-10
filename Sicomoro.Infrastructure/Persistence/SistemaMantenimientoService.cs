using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sicomoro.Application.DTOs;
using Sicomoro.Application.Interfaces;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Infrastructure.Persistence;

public sealed class SistemaMantenimientoService(SicomoroDbContext db) : ISistemaMantenimientoService
{
    public async Task<LimpiezaSistemaDto> ReiniciarDatosOperativosAsync(Guid? usuarioId, CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var documentos = await db.DocumentosVenta.ExecuteDeleteAsync(cancellationToken);
        var pagos = await db.Pagos.ExecuteDeleteAsync(cancellationToken);
        var cobros = await db.Cobros.ExecuteDeleteAsync(cancellationToken);
        var caja = await db.CajaMovimientos.ExecuteDeleteAsync(cancellationToken);
        var cajaCierres = await db.CajaCierres.ExecuteDeleteAsync(cancellationToken);
        var movimientos = await db.MovimientosInventario.ExecuteDeleteAsync(cancellationToken);
        var compraDetalles = await db.CompraDetalles.ExecuteDeleteAsync(cancellationToken);
        var ventaDetalles = await db.VentaDetalles.ExecuteDeleteAsync(cancellationToken);
        var transportes = await db.Transportes.ExecuteDeleteAsync(cancellationToken);
        var compras = await db.Compras.ExecuteDeleteAsync(cancellationToken);
        var ventas = await db.Ventas.ExecuteDeleteAsync(cancellationToken);
        var inventario = await db.Inventario.ExecuteDeleteAsync(cancellationToken);
        var anuncios = await db.AnunciosCatalogo.ExecuteDeleteAsync(cancellationToken);
        var productos = await db.ProductosMadera.ExecuteDeleteAsync(cancellationToken);
        var clientes = await db.Clientes.ExecuteDeleteAsync(cancellationToken);
        var proveedores = await db.Proveedores.ExecuteDeleteAsync(cancellationToken);
        var notificaciones = await db.Notificaciones.ExecuteDeleteAsync(cancellationToken);
        var auditoria = await db.Auditoria.ExecuteDeleteAsync(cancellationToken);

        var usuariosEliminados = usuarioId is null
            ? await db.Users.Where(x => x.Email == "admin@sicomoro.local" || x.Rol != RolSistema.Administrador).ExecuteDeleteAsync(cancellationToken)
            : await db.Users.Where(x => x.Id != usuarioId.Value).ExecuteDeleteAsync(cancellationToken);
        var administradores = await db.Users.CountAsync(x => x.Rol == RolSistema.Administrador, cancellationToken);

        var resultado = new LimpiezaSistemaDto(
            clientes,
            proveedores,
            productos,
            inventario,
            movimientos,
            compras,
            compraDetalles,
            transportes,
            ventas,
            ventaDetalles,
            cobros,
            pagos,
            caja,
            cajaCierres,
            documentos,
            anuncios,
            notificaciones,
            auditoria,
            usuariosEliminados,
            administradores);

        db.Auditoria.Add(new Auditoria(usuarioId, "ReinicioDatosOperativos", "Sistema", null, null, JsonSerializer.Serialize(resultado)));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return resultado;
    }
}
