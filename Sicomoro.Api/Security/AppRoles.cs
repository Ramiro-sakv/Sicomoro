namespace Sicomoro.Api.Security;

public static class AppRoles
{
    public const string Admin = "Administrador";
    public const string Gerente = "Gerente";
    public const string Vendedor = "Vendedor";
    public const string Inventario = "Inventario";
    public const string Cobrador = "Cobrador";

    public const string Staff = $"{Admin},{Gerente},{Vendedor},{Inventario},{Cobrador}";
    public const string Gestion = $"{Admin},{Gerente}";
    public const string Ventas = $"{Admin},{Gerente},{Vendedor}";
    public const string InventarioGestion = $"{Admin},{Gerente},{Inventario}";
    public const string Cobranza = $"{Admin},{Gerente},{Cobrador}";
}
