using ChatService.Application.Abstractions;
using ChatService.Domain.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork, IUnitOfWorkExecutor
{
    private readonly ConversationDbContext _db;
    private readonly IMediator _mediator;

    public EfUnitOfWork(ConversationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public async Task<int> SaveChangesWithDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = CollectDomainEvents();
        var n = await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in events)
            await _mediator.Publish(e, cancellationToken);
        ClearDomainEvents();
        return n;
    }

    public async Task<TResult> RunAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            var events = CollectDomainEvents();
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            foreach (var e in events)
                await _mediator.Publish(e, cancellationToken);
            ClearDomainEvents();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var list = new List<IDomainEvent>();
        foreach (var entry in _db.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached)
                continue;
            if (entry.Entity is IHasDomainEvents agg)
                list.AddRange(agg.DomainEvents);
        }

        return list;
    }

    private void ClearDomainEvents()
    {
        foreach (var entry in _db.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached)
                continue;
            if (entry.Entity is IHasDomainEvents agg)
                agg.ClearDomainEvents();
        }
    }
}
