using FluentAssertions;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;
using Sicomoro.Domain.Events;

namespace Sicomoro.Tests.UnitTests;

public sealed class ReglasDeNegocioTests
{
    [Fact]
    public void Inventario_NoPermiteDescontarMasStockDelDisponible()
    {
        var producto = new ProductoMadera("Tajibo 2x4", "Tajibo", UnidadMedida.Pieza, 2, 4, 0, "A", 30, 55, 5, null);
        var inventario = new Inventario(producto.Id, "A1");
        inventario.Incrementar(3);

        var act = () => inventario.Descontar(4);

        act.Should().Throw<InvalidOperationException>().WithMessage("*stock*");
    }

    [Fact]
    public void Venta_NoPermiteConfirmarSinDetalle()
    {
        var venta = new Venta(Guid.NewGuid(), Guid.NewGuid(), MetodoPago.Credito, DateTime.UtcNow.AddDays(15), null);

        var act = () => venta.Confirmar(0);

        act.Should().Throw<InvalidOperationException>().WithMessage("*detalle*");
    }

    [Fact]
    public void Venta_NoPermitePagoMayorAlSaldoPendiente()
    {
        var venta = new Venta(Guid.NewGuid(), Guid.NewGuid(), MetodoPago.Credito, DateTime.UtcNow.AddDays(15), null);
        venta.AgregarDetalle(Guid.NewGuid(), 2, 100, 0);
        venta.Confirmar(20);

        var act = () => venta.RegistrarPago(500);

        act.Should().Throw<InvalidOperationException>().WithMessage("*saldo pendiente*");
    }

    [Fact]
    public void VentaAnulada_NoPermiteRegistrarPagos()
    {
        var venta = new Venta(Guid.NewGuid(), Guid.NewGuid(), MetodoPago.Credito, DateTime.UtcNow.AddDays(15), null);
        venta.AgregarDetalle(Guid.NewGuid(), 1, 100, 0);
        venta.Confirmar(0);
        venta.Anular("Error de carga");

        var act = () => venta.RegistrarPago(10);

        act.Should().Throw<InvalidOperationException>().WithMessage("*anulada*");
    }

    [Fact]
    public void Cobro_NoPermitePagoMayorAlSaldo()
    {
        var cobro = new Cobro(Guid.NewGuid(), Guid.NewGuid(), 100, DateTime.UtcNow.AddDays(10));

        var act = () => cobro.RegistrarPago(101, MetodoPago.Efectivo, Guid.NewGuid(), null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*saldo pendiente*");
    }
}

