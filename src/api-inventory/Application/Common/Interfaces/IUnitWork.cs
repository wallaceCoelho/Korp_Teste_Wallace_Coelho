using System.Data;

namespace Application.Common.Interfaces;

public interface IUnitWork
{
    IQueryable<TEntity> AsQueryable<TEntity>() where TEntity : class;
    Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    TEntity Update<TEntity>(TEntity entity) where TEntity : class;
    TEntity Delete<TEntity>(TEntity entity) where TEntity : class;
    Task ReloadAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    void Detach<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
