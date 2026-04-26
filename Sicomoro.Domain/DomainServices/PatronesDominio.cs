using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Domain.DomainServices;

public interface IPricingStrategy
{
    string Codigo { get; }
    decimal CalcularPrecio(ProductoMadera producto, decimal cantidad, decimal descuentoManual);
}

public sealed class PrecioNormalStrategy : IPricingStrategy
{
    public string Codigo => "normal";
    public decimal CalcularPrecio(ProductoMadera producto, decimal cantidad, decimal descuentoManual) => producto.PrecioVentaSugerido;
}

public sealed class PrecioMayoristaStrategy : IPricingStrategy
{
    public string Codigo => "mayorista";
    public decimal CalcularPrecio(ProductoMadera producto, decimal cantidad, decimal descuentoManual) =>
        cantidad >= 50 ? Math.Round(producto.PrecioVentaSugerido * 0.95m, 2) : producto.PrecioVentaSugerido;
}

public sealed class PrecioClienteFrecuenteStrategy : IPricingStrategy
{
    public string Codigo => "cliente-frecuente";
    public decimal CalcularPrecio(ProductoMadera producto, decimal cantidad, decimal descuentoManual) =>
        Math.Round(producto.PrecioVentaSugerido * 0.97m, 2);
}

public sealed class PrecioConDescuentoManualStrategy : IPricingStrategy
{
    public string Codigo => "descuento-manual";
    public decimal CalcularPrecio(ProductoMadera producto, decimal cantidad, decimal descuentoManual) =>
        Math.Max(0, producto.PrecioVentaSugerido - descuentoManual);
}

public sealed class PricingService(IEnumerable<IPricingStrategy> strategies)
{
    public decimal Calcular(string codigo, ProductoMadera producto, decimal cantidad, decimal descuentoManual = 0)
    {
        var strategy = strategies.FirstOrDefault(x => x.Codigo == codigo) ?? strategies.First(x => x.Codigo == "normal");
        return strategy.CalcularPrecio(producto, cantidad, descuentoManual);
    }
}

public interface IEstadoVentaPolicy
{
    EstadoVenta Estado { get; }
    void ValidarPermitePago();
    void ValidarPermiteAnulacion();
}

public sealed class VentaAnuladaPolicy : IEstadoVentaPolicy
{
    public EstadoVenta Estado => EstadoVenta.Anulada;
    public void ValidarPermitePago() => throw new InvalidOperationException("Una venta anulada no debe permitir pagos nuevos.");
    public void ValidarPermiteAnulacion() { }
}

public sealed class VentaActivaPolicy(EstadoVenta estado) : IEstadoVentaPolicy
{
    public EstadoVenta Estado { get; } = estado;
    public void ValidarPermitePago()
    {
        if (Estado == EstadoVenta.Pagada) throw new InvalidOperationException("Una venta pagada no requiere pagos nuevos.");
    }
    public void ValidarPermiteAnulacion() { }
}

public sealed class EstadoVentaFactory
{
    public IEstadoVentaPolicy Crear(EstadoVenta estado) => estado == EstadoVenta.Anulada ? new VentaAnuladaPolicy() : new VentaActivaPolicy(estado);
}

