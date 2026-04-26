using Sicomoro.Application.Interfaces;

namespace Sicomoro.Infrastructure.ExternalServices;

public sealed class EmailSenderAdapter : IEmailSender
{
    public Task EnviarAsync(string destino, string asunto, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class WhatsAppSenderAdapter : IWhatsAppSender
{
    public Task EnviarAsync(string telefono, string mensaje, string? adjunto = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

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

