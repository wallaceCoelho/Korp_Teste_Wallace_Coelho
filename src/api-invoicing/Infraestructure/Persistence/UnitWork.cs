using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infraestructure.Persistence;

internal class UnitWork(InvoicingDbContext context) : IUnitWork
{
    public IQueryable<TEntity> AsQueryable<TEntity>() where TEntity : class
    {
        return context.Set<TEntity>();
    }

    public async Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        await context.Set<TEntity>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public TEntity Update<TEntity>(TEntity entity) where TEntity : class
    {
        context.Set<TEntity>().Update(entity);
        return entity;
    }

    public TEntity Delete<TEntity>(TEntity entity) where TEntity : class
    {
        context.Set<TEntity>().Remove(entity);
        return entity;
    }

    public async Task ReloadAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        await context.Entry(entity).ReloadAsync(cancellationToken);
    }

    public void Detach<TEntity>(TEntity entity) where TEntity : class
    {
        context.Entry(entity).State = EntityState.Detached;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxAttempts)
            {
                foreach (var entry in ex.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues is null)
                    {
                        return 0;
                    }

                    entry.OriginalValues.SetValues(databaseValues);
                }
            }
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return result;
        }

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            try
            {
                var result = await operation(cancellationToken);

                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, isolationLevel, cancellationToken);
    }
}
