using System.Text;
using Microsoft.Extensions.Configuration;
using Sicomoro.Application.Interfaces;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Infrastructure.Pdf;

public abstract class DocumentoPdfTemplate
{
    public byte[] Generar(Venta venta, string numero, Guid usuarioId)
    {
        var lineas = new List<string>();
        AgregarEncabezado(lineas, numero, usuarioId);
        AgregarDatosCliente(lineas, venta);
        AgregarDetalle(lineas, venta);
        AgregarTotales(lineas, venta);
        AgregarPie(lineas);
        return SimplePdfWriter.Write(lineas);
    }

    protected virtual void AgregarEncabezado(List<string> lineas, string numero, Guid usuarioId)
    {
        lineas.Add("Barraca Sicomoro");
        lineas.Add("Comprobante interno de venta");
        lineas.Add($"Documento: {numero}");
        lineas.Add($"Fecha: {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        lineas.Add($"Usuario generador: {usuarioId}");
        lineas.Add("----------------------------------------");
    }

    protected virtual void AgregarDatosCliente(List<string> lineas, Venta venta)
    {
        lineas.Add($"Cliente: {venta.Cliente?.NombreRazonSocial ?? venta.ClienteId.ToString()}");
        lineas.Add($"CI/NIT: {venta.Cliente?.CiNit ?? "-"}");
        lineas.Add("----------------------------------------");
    }

    protected abstract void AgregarDetalle(List<string> lineas, Venta venta);

    protected virtual void AgregarTotales(List<string> lineas, Venta venta)
    {
        lineas.Add("----------------------------------------");
        lineas.Add($"Total: {venta.Total:N2}");
        lineas.Add($"Pagado: {venta.MontoPagado:N2}");
        lineas.Add($"Saldo: {venta.SaldoPendiente:N2}");
    }

    protected virtual void AgregarPie(List<string> lineas) => lineas.Add("Documento interno sin validez fiscal oficial.");
}

public sealed class ComprobanteVentaPdf : DocumentoPdfTemplate
{
    protected override void AgregarDetalle(List<string> lineas, Venta venta)
    {
        lineas.Add("Detalle de productos:");
        foreach (var d in venta.Detalles)
            lineas.Add($"- {d.ProductoMadera?.NombreComercial ?? d.ProductoMaderaId.ToString()} | Cant: {d.Cantidad:N2} | P/U: {d.PrecioUnitario:N2} | Desc: {d.Descuento:N2} | Subtotal: {d.Subtotal:N2}");
    }
}

public sealed class ReciboPagoPdf : DocumentoPdfTemplate
{
    protected override void AgregarDetalle(List<string> lineas, Venta venta) => lineas.Add($"Recibo de pago asociado a venta {venta.Id}.");
}

public sealed class NotaEntregaPdf : DocumentoPdfTemplate
{
    protected override void AgregarDetalle(List<string> lineas, Venta venta)
    {
        lineas.Add("Nota de entrega:");
        foreach (var d in venta.Detalles) lineas.Add($"- {d.ProductoMadera?.NombreComercial ?? d.ProductoMaderaId.ToString()} | Cant: {d.Cantidad:N2}");
    }
}

public sealed class PdfComprobanteProvider(IConfiguration configuration, IEmailSender email) : IFacturacionProvider
{
    public async Task<DocumentoVenta> GenerarDocumentoVentaAsync(Venta venta, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var numero = $"SIC-{DateTime.UtcNow:yyyyMMdd}-{venta.Id.ToString()[..8]}";
        var basePath = configuration["Storage:DocumentosPath"] ?? Path.Combine(AppContext.BaseDirectory, "documentos");
        Directory.CreateDirectory(basePath);
        var path = Path.Combine(basePath, $"{numero}.pdf");
        var bytes = new ComprobanteVentaPdf().Generar(venta, numero, usuarioId);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return new DocumentoVenta(venta.Id, TipoDocumentoVenta.ComprobanteVenta, numero, path, usuarioId);
    }

    public Task EnviarDocumentoAsync(DocumentoVenta documento, string destino, CancellationToken cancellationToken = default) =>
        email.EnviarAsync(destino, $"Comprobante {documento.Numero}", "Adjuntamos su comprobante Sicomoro.", documento.RutaArchivo, cancellationToken);

    public Task AnularDocumentoAsync(DocumentoVenta documento, string motivo, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class FacturacionElectronicaProvider : IFacturacionProvider
{
    public Task<DocumentoVenta> GenerarDocumentoVentaAsync(Venta venta, Guid usuarioId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Facturacion electronica pendiente de configuracion oficial.");
    public Task EnviarDocumentoAsync(DocumentoVenta documento, string destino, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Facturacion electronica pendiente de configuracion oficial.");
    public Task AnularDocumentoAsync(DocumentoVenta documento, string motivo, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Facturacion electronica pendiente de configuracion oficial.");
}

public sealed class DocumentoFactory(PdfComprobanteProvider pdfProvider, FacturacionElectronicaProvider fiscalProvider) : IDocumentoFactory
{
    public IFacturacionProvider Crear(TipoDocumentoVenta tipo) => tipo == TipoDocumentoVenta.DocumentoFiscalFuturo ? fiscalProvider : pdfProvider;
}

public sealed class LoggingDocumentoServiceDecorator(IFacturacionProvider inner) : IFacturacionProvider
{
    public Task<DocumentoVenta> GenerarDocumentoVentaAsync(Venta venta, Guid usuarioId, CancellationToken cancellationToken = default) => inner.GenerarDocumentoVentaAsync(venta, usuarioId, cancellationToken);
    public Task EnviarDocumentoAsync(DocumentoVenta documento, string destino, CancellationToken cancellationToken = default) => inner.EnviarDocumentoAsync(documento, destino, cancellationToken);
    public Task AnularDocumentoAsync(DocumentoVenta documento, string motivo, CancellationToken cancellationToken = default) => inner.AnularDocumentoAsync(documento, motivo, cancellationToken);
}

internal static class SimplePdfWriter
{
    public static byte[] Write(IReadOnlyList<string> lines)
    {
        var content = "BT /F1 11 Tf 50 780 Td " + string.Join(" T* ", lines.Select(EscapePdf)) + " ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"
        };
        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(offsets.Count - 1).Append(" 0 obj\n").Append(obj).Append("\nendobj\n");
        }
        var xref = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var off in offsets.Skip(1)) sb.Append(off.ToString("0000000000")).Append(" 00000 n \n");
        sb.Append("trailer << /Size 6 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string EscapePdf(string value)
    {
        var clean = value.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
        return $"({clean.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")})";
    }
}
