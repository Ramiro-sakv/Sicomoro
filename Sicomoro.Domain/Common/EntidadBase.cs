namespace Sicomoro.Domain.Common;

public abstract class EntidadBase
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreadoEn { get; protected set; } = DateTime.UtcNow;
    public DateTime? ActualizadoEn { get; protected set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void MarcarActualizado() => ActualizadoEn = DateTime.UtcNow;
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent
{
    DateTime OcurridoEn { get; }
}

