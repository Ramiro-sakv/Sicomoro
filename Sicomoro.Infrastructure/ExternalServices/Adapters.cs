using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sicomoro.Application.Interfaces;

namespace Sicomoro.Infrastructure.ExternalServices;

public sealed class EmailSenderAdapter : IEmailSender
{
    public Task EnviarAsync(string destino, string asunto, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class WhatsAppSenderAdapter : IWhatsAppSender
{
    public Task EnviarAsync(string telefono, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnviarPlantillaAsync(string telefono, string plantilla, string codigoIdioma = "en_US", IReadOnlyCollection<string>? parametros = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class WhatsAppCloudOptions
{
    public bool Enabled { get; init; }
    public string ApiVersion { get; init; } = "v25.0";
    public string? AccessToken { get; init; }
    public string? PhoneNumberId { get; init; }
    public string? OwnerPhoneNumber { get; init; }
}

public sealed class WhatsAppCloudSender(HttpClient httpClient, IOptions<WhatsAppCloudOptions> options) : IWhatsAppSender
{
    private readonly WhatsAppCloudOptions _options = options.Value;

    public Task EnviarAsync(string telefono, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default)
    {
        var payload = new WhatsAppTextRequest(
            To: NormalizePhone(telefono),
            Text: new WhatsAppTextBody(false, mensaje));

        return PostMessageAsync(payload, cancellationToken);
    }

    public Task EnviarPlantillaAsync(string telefono, string plantilla, string codigoIdioma = "en_US", IReadOnlyCollection<string>? parametros = null, CancellationToken cancellationToken = default)
    {
        var components = parametros is { Count: > 0 }
            ? [new WhatsAppTemplateComponent("body", parametros.Select(x => new WhatsAppTemplateParameter("text", x)).ToArray())]
            : Array.Empty<WhatsAppTemplateComponent>();

        var payload = new WhatsAppTemplateRequest(
            To: NormalizePhone(telefono),
            Template: new WhatsAppTemplateBody(plantilla, new WhatsAppLanguage(codigoIdioma), components));

        return PostMessageAsync(payload, cancellationToken);
    }

    private async Task PostMessageAsync(object payload, CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return;
        if (string.IsNullOrWhiteSpace(_options.AccessToken) || string.IsNullOrWhiteSpace(_options.PhoneNumberId))
            throw new InvalidOperationException("WhatsApp Cloud API no esta configurado.");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"WhatsApp Cloud API rechazo el mensaje ({(int)response.StatusCode}): {body}");
        }
    }

    private static string NormalizePhone(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}

public sealed class BusinessAlertService(IWhatsAppSender whatsApp, IOptions<WhatsAppCloudOptions> options, ILogger<BusinessAlertService> logger) : IBusinessAlertService
{
    private readonly WhatsAppCloudOptions _options = options.Value;

    public Task EnviarPruebaAsync(string mensaje, CancellationToken cancellationToken = default) =>
        SendOwnerAsync($"Sicomoro prueba WhatsApp\n{mensaje}", cancellationToken);

    public Task VentaConfirmadaAsync(Guid ventaId, string cliente, decimal total, decimal pagado, decimal saldo, CancellationToken cancellationToken = default) =>
        SendOwnerAsync($"""
        Sicomoro: Venta confirmada
        Cliente: {cliente}
        Total: Bs {total:N2}
        Pagado: Bs {pagado:N2}
        Saldo: Bs {saldo:N2}
        Venta: {ventaId}
        """, cancellationToken);

    public Task CompraRecibidaAsync(Guid compraId, string proveedor, string origen, decimal totalProductos, CancellationToken cancellationToken = default) =>
        SendOwnerAsync($"""
        Sicomoro: Compra recibida
        Proveedor: {proveedor}
        Origen: {origen}
        Total productos: Bs {totalProductos:N2}
        Compra: {compraId}
        """, cancellationToken);

    public Task PagoRegistradoAsync(Guid ventaId, string cliente, decimal monto, decimal saldo, CancellationToken cancellationToken = default) =>
        SendOwnerAsync($"""
        Sicomoro: Pago registrado
        Cliente: {cliente}
        Monto: Bs {monto:N2}
        Saldo restante: Bs {saldo:N2}
        Venta: {ventaId}
        """, cancellationToken);

    public Task InventarioAjustadoAsync(string producto, decimal stockActual, decimal stockMinimo, string motivo, CancellationToken cancellationToken = default) =>
        SendOwnerAsync($"""
        Sicomoro: Movimiento de inventario
        Producto: {producto}
        Stock actual: {stockActual:N2}
        Stock minimo: {stockMinimo:N2}
        Motivo: {motivo}
        """, cancellationToken);

    public Task VentaAnuladaAsync(Guid ventaId, string motivo, CancellationToken cancellationToken = default) =>
        SendOwnerAsync($"""
        Sicomoro: Venta anulada
        Venta: {ventaId}
        Motivo: {motivo}
        """, cancellationToken);

    private async Task SendOwnerAsync(string mensaje, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.OwnerPhoneNumber)) return;

        try
        {
            await whatsApp.EnviarAsync(_options.OwnerPhoneNumber, mensaje, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo enviar alerta WhatsApp al dueño.");
        }
    }
}

public sealed record WhatsAppTextRequest(
    [property: JsonPropertyName("messaging_product")] string MessagingProduct = "whatsapp",
    [property: JsonPropertyName("recipient_type")] string RecipientType = "individual",
    [property: JsonPropertyName("to")] string To = "",
    [property: JsonPropertyName("type")] string Type = "text",
    [property: JsonPropertyName("text")] WhatsAppTextBody? Text = null);

public sealed record WhatsAppTextBody(
    [property: JsonPropertyName("preview_url")] bool PreviewUrl,
    [property: JsonPropertyName("body")] string Body);

public sealed record WhatsAppTemplateRequest(
    [property: JsonPropertyName("messaging_product")] string MessagingProduct = "whatsapp",
    [property: JsonPropertyName("to")] string To = "",
    [property: JsonPropertyName("type")] string Type = "template",
    [property: JsonPropertyName("template")] WhatsAppTemplateBody? Template = null);

public sealed record WhatsAppTemplateBody(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("language")] WhatsAppLanguage Language,
    [property: JsonPropertyName("components")] IReadOnlyCollection<WhatsAppTemplateComponent> Components);

public sealed record WhatsAppLanguage([property: JsonPropertyName("code")] string Code);
public sealed record WhatsAppTemplateComponent([property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("parameters")] IReadOnlyCollection<WhatsAppTemplateParameter> Parameters);
public sealed record WhatsAppTemplateParameter([property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("text")] string Text);

public sealed class RetryEmailSenderDecorator(IEmailSender inner) : IEmailSender
{
    public async Task EnviarAsync(string destino, string asunto, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default)
    {
        for (var intento = 1; ; intento++)
        {
            try { await inner.EnviarAsync(destino, asunto, mensaje, adjunto, cancellationToken); return; }
            catch when (intento < 3) { await Task.Delay(200 * intento, cancellationToken); }
        }
    }
}

public sealed class FacturacionElectronicaAdapter
{
    public Task EnviarAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Pendiente de configuracion fiscal oficial.");
}
