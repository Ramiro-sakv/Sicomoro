using System.Text;
using Microsoft.Extensions.Configuration;
using Sicomoro.Application.Interfaces;
using Sicomoro.Domain.Entities;
using Sicomoro.Domain.Enums;

namespace Sicomoro.Infrastructure.Pdf;

public abstract class DocumentoPdfTemplate
{
    public virtual byte[] Generar(Venta venta, string numero, Guid usuarioId, string usuarioNombre)
    {
        var lineas = new List<string>();
        AgregarEncabezado(lineas, numero, usuarioNombre);
        AgregarDatosCliente(lineas, venta);
        AgregarDetalle(lineas, venta);
        AgregarTotales(lineas, venta);
        AgregarPie(lineas);
        return SimplePdfWriter.Write(lineas);
    }

    protected virtual void AgregarEncabezado(List<string> lineas, string numero, string usuarioNombre)
    {
        lineas.Add("Barraca Sicomoro");
        lineas.Add("Comprobante interno de venta");
        lineas.Add($"Documento: {numero}");
        lineas.Add($"Fecha: {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        lineas.Add($"Usuario generador: {usuarioNombre}");
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
    public override byte[] Generar(Venta venta, string numero, Guid usuarioId, string usuarioNombre) =>
        SimplePdfWriter.WriteComprobante(venta, numero, usuarioNombre);

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
    public async Task<DocumentoVenta> GenerarDocumentoVentaAsync(Venta venta, Guid usuarioId, string usuarioNombre, CancellationToken cancellationToken = default)
    {
        var numero = $"SIC-{DateTime.UtcNow:yyyyMMdd}-{venta.Id.ToString()[..8]}";
        var basePath = configuration["Storage:DocumentosPath"] ?? Path.Combine(AppContext.BaseDirectory, "documentos");
        Directory.CreateDirectory(basePath);
        var path = Path.Combine(basePath, $"{numero}.pdf");
        var bytes = new ComprobanteVentaPdf().Generar(venta, numero, usuarioId, usuarioNombre);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return new DocumentoVenta(venta.Id, TipoDocumentoVenta.ComprobanteVenta, numero, path, usuarioId);
    }

    public Task EnviarDocumentoAsync(DocumentoVenta documento, string destino, CancellationToken cancellationToken = default) =>
        email.EnviarAsync(destino, $"Comprobante {documento.Numero}", "Adjuntamos su comprobante Sicomoro.", documento.RutaArchivo, cancellationToken);

    public Task AnularDocumentoAsync(DocumentoVenta documento, string motivo, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class FacturacionElectronicaProvider : IFacturacionProvider
{
    public Task<DocumentoVenta> GenerarDocumentoVentaAsync(Venta venta, Guid usuarioId, string usuarioNombre, CancellationToken cancellationToken = default) =>
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
    public Task<DocumentoVenta> GenerarDocumentoVentaAsync(Venta venta, Guid usuarioId, string usuarioNombre, CancellationToken cancellationToken = default) => inner.GenerarDocumentoVentaAsync(venta, usuarioId, usuarioNombre, cancellationToken);
    public Task EnviarDocumentoAsync(DocumentoVenta documento, string destino, CancellationToken cancellationToken = default) => inner.EnviarDocumentoAsync(documento, destino, cancellationToken);
    public Task AnularDocumentoAsync(DocumentoVenta documento, string motivo, CancellationToken cancellationToken = default) => inner.AnularDocumentoAsync(documento, motivo, cancellationToken);
}

internal static class SimplePdfWriter
{
    public static byte[] WriteComprobante(Venta venta, string numero, string usuarioNombre)
    {
        var pdf = new StringBuilder();
        AddRect(pdf, 0, 0, 612, 792, "0.98 0.99 0.98");
        AddRect(pdf, 36, 704, 540, 58, "0.13 0.36 0.25");
        AddText(pdf, "SICOMORO", 52, 736, 22, true, "1 1 1");
        AddText(pdf, "Barraca de madera", 52, 718, 10, false, "0.88 0.95 0.90");
        AddText(pdf, "COMPROBANTE INTERNO", 390, 738, 11, true, "1 1 1");
        AddText(pdf, numero, 390, 720, 10, true, "1 1 1");

        AddRect(pdf, 36, 608, 540, 76, "1 1 1", "0.82 0.87 0.84");
        AddText(pdf, "Cliente", 52, 662, 9, true, "0.25 0.33 0.30");
        AddText(pdf, venta.Cliente?.NombreRazonSocial ?? venta.ClienteId.ToString(), 52, 646, 12, true, "0.08 0.12 0.10");
        AddText(pdf, $"CI/NIT: {venta.Cliente?.CiNit ?? "-"}", 52, 629, 10, false, "0.25 0.33 0.30");
        AddText(pdf, $"Fecha: {DateTime.UtcNow:yyyy-MM-dd HH:mm}", 380, 662, 10, false, "0.25 0.33 0.30");
        AddText(pdf, $"Generado por: {usuarioNombre}", 380, 646, 10, false, "0.25 0.33 0.30");
        AddText(pdf, $"Venta: {venta.Id.ToString()[..8]}", 380, 629, 10, false, "0.25 0.33 0.30");

        AddText(pdf, "Detalle de productos", 42, 578, 13, true, "0.08 0.12 0.10");
        AddRect(pdf, 36, 548, 540, 24, "0.90 0.94 0.91", "0.78 0.84 0.80");
        AddText(pdf, "Producto", 48, 556, 9, true, "0.14 0.26 0.20");
        AddText(pdf, "Cant.", 306, 556, 9, true, "0.14 0.26 0.20");
        AddText(pdf, "P/U", 365, 556, 9, true, "0.14 0.26 0.20");
        AddText(pdf, "Desc.", 430, 556, 9, true, "0.14 0.26 0.20");
        AddText(pdf, "Subtotal", 500, 556, 9, true, "0.14 0.26 0.20");

        var y = 526;
        foreach (var detalle in venta.Detalles.Take(11))
        {
            var producto = detalle.ProductoMadera?.NombreComercial ?? $"Producto {detalle.ProductoMaderaId.ToString()[..8]}";
            AddLine(pdf, 36, y + 16, 576, y + 16, "0.88 0.91 0.89");
            AddText(pdf, Truncate(producto, 44), 48, y, 9, false, "0.08 0.12 0.10");
            AddText(pdf, Qty(detalle.Cantidad), 306, y, 9, false, "0.08 0.12 0.10");
            AddText(pdf, Money(detalle.PrecioUnitario), 365, y, 9, false, "0.08 0.12 0.10");
            AddText(pdf, Money(detalle.Descuento), 430, y, 9, false, "0.08 0.12 0.10");
            AddText(pdf, Money(detalle.Subtotal), 500, y, 9, true, "0.08 0.12 0.10");
            y -= 24;
        }

        if (venta.Detalles.Count > 11)
            AddText(pdf, $"+ {venta.Detalles.Count - 11} productos mas en esta venta", 48, y, 9, true, "0.55 0.35 0.08");

        AddRect(pdf, 352, 148, 224, 90, "1 1 1", "0.78 0.84 0.80");
        AddText(pdf, "Resumen", 370, 214, 11, true, "0.08 0.12 0.10");
        AddText(pdf, "Total", 370, 194, 10, false, "0.25 0.33 0.30");
        AddText(pdf, Money(venta.Total), 488, 194, 11, true, "0.08 0.12 0.10");
        AddText(pdf, "Pagado", 370, 174, 10, false, "0.25 0.33 0.30");
        AddText(pdf, Money(venta.MontoPagado), 488, 174, 11, true, "0.08 0.12 0.10");
        AddText(pdf, "Saldo", 370, 154, 10, false, "0.25 0.33 0.30");
        AddText(pdf, Money(venta.SaldoPendiente), 488, 154, 11, true, venta.SaldoPendiente > 0 ? "0.65 0.25 0.20" : "0.13 0.36 0.25");

        AddRect(pdf, 36, 148, 284, 90, "0.96 0.98 0.96", "0.82 0.87 0.84");
        AddText(pdf, "Observacion", 52, 214, 10, true, "0.08 0.12 0.10");
        AddText(pdf, "Documento interno para control de venta y entrega.", 52, 194, 9, false, "0.25 0.33 0.30");
        AddText(pdf, "No reemplaza factura fiscal oficial.", 52, 178, 9, false, "0.65 0.25 0.20");
        AddText(pdf, "Gracias por su compra.", 52, 162, 9, true, "0.13 0.36 0.25");

        AddLine(pdf, 36, 104, 576, 104, "0.78 0.84 0.80");
        AddText(pdf, "Sicomoro - control interno de barraca de madera", 42, 84, 9, false, "0.36 0.45 0.41");

        return BuildPdf(pdf.ToString());
    }

    public static byte[] Write(IReadOnlyList<string> lines)
    {
        var preparedLines = lines
            .SelectMany(line => WrapLine(NormalizeText(line), 92))
            .Take(42)
            .ToList();
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("q 0.94 0.97 0.95 rg 36 706 540 56 re f Q");
        contentBuilder.AppendLine("0.18 0.44 0.31 RG 1.5 w 36 706 540 56 re S");

        var y = 736;
        for (var i = 0; i < preparedLines.Count; i++)
        {
            var font = i == 0 ? "/F2 18 Tf" : i == 1 ? "/F2 12 Tf" : "/F1 10.5 Tf";
            contentBuilder.Append("BT ")
                .Append(font)
                .Append(" 50 ")
                .Append(y)
                .Append(" Td ")
                .Append(EscapePdf(preparedLines[i]))
                .AppendLine(" Tj ET");
            y -= i < 2 ? 18 : 15;
        }

        var content = contentBuilder.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
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
        sb.Append("xref\n0 7\n0000000000 65535 f \n");
        foreach (var off in offsets.Skip(1)) sb.Append(off.ToString("0000000000")).Append(" 00000 n \n");
        sb.Append("trailer << /Size 7 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static byte[] BuildPdf(string content)
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
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
        sb.Append("xref\n0 7\n0000000000 65535 f \n");
        foreach (var off in offsets.Skip(1)) sb.Append(off.ToString("0000000000")).Append(" 00000 n \n");
        sb.Append("trailer << /Size 7 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static void AddText(StringBuilder sb, string value, int x, int y, int size, bool bold, string color)
    {
        sb.Append("BT ")
            .Append(color)
            .Append(" rg ")
            .Append(bold ? "/F2 " : "/F1 ")
            .Append(size)
            .Append(" Tf ")
            .Append(x)
            .Append(' ')
            .Append(y)
            .Append(" Td ")
            .Append(EscapePdf(NormalizeText(value)))
            .AppendLine(" Tj ET");
    }

    private static void AddRect(StringBuilder sb, int x, int y, int width, int height, string fill, string? stroke = null)
    {
        sb.Append("q ")
            .Append(fill)
            .Append(" rg ")
            .Append(x)
            .Append(' ')
            .Append(y)
            .Append(' ')
            .Append(width)
            .Append(' ')
            .Append(height)
            .Append(" re f");

        if (!string.IsNullOrWhiteSpace(stroke))
        {
            sb.Append(' ')
                .Append(stroke)
                .Append(" RG 1 w ")
                .Append(x)
                .Append(' ')
                .Append(y)
                .Append(' ')
                .Append(width)
                .Append(' ')
                .Append(height)
                .Append(" re S");
        }

        sb.AppendLine(" Q");
    }

    private static void AddLine(StringBuilder sb, int x1, int y1, int x2, int y2, string stroke)
    {
        sb.Append("q ")
            .Append(stroke)
            .Append(" RG 0.7 w ")
            .Append(x1)
            .Append(' ')
            .Append(y1)
            .Append(" m ")
            .Append(x2)
            .Append(' ')
            .Append(y2)
            .AppendLine(" l S Q");
    }

    private static string Money(decimal value) =>
        value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("es-BO"));

    private static string Qty(decimal value) =>
        value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("es-BO"));

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 3)] + "...";

    private static IEnumerable<string> WrapLine(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            yield return value;
            yield break;
        }

        for (var i = 0; i < value.Length; i += maxLength)
            yield return value.Substring(i, Math.Min(maxLength, value.Length - i));
    }

    private static string NormalizeText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark && c <= 127)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string EscapePdf(string value)
    {
        return $"({value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")})";
    }
}
