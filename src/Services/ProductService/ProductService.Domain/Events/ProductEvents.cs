using SharedKernel;

namespace ProductService.Domain.Events;

public record ProductCreatedEvent(Guid ProductId, string Name, decimal Price) : IDomainEvent;

public record ProductUpdatedEvent(Guid ProductId, string Name) : IDomainEvent;

public record ProductDeletedEvent(Guid ProductId) : IDomainEvent;
