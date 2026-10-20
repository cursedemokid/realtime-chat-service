namespace ChatService.Application.Abstractions;

public interface IUnitOfWorkExecutor
{
    Task<TResult> RunAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
