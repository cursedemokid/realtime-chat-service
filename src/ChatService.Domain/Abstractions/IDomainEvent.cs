using MediatR;

namespace ChatService.Domain.Abstractions;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
