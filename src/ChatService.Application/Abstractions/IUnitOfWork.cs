namespace ChatService.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesWithDomainEventsAsync(CancellationToken cancellationToken = default);
}
