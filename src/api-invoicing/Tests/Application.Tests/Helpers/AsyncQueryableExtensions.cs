using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Application.Tests.Helpers;

public static class AsyncQueryableExtensions
{
    public static IQueryable<T> BuildMockDbSet<T>(this IEnumerable<T> source) where T : class
    {
        return new TestAsyncEnumerable<T>(source);
    }

    private class TestAsyncEnumerable<T>(IEnumerable<T> enumerable) : EnumerableQuery<T>(enumerable), IAsyncEnumerable<T>, IQueryable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private class TestAsyncEnumerator<T>(IEnumerator<T> enumerator) : IAsyncEnumerator<T>
    {
        public T Current => enumerator.Current;

        public ValueTask DisposeAsync()
        {
            enumerator.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(enumerator.MoveNext());
        }
    }

    private class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>((IEnumerable<TEntity>)inner.CreateQuery(expression));

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(inner.CreateQuery<TElement>(expression));
        }

        public object? Execute(Expression expression) => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethods()
                .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, [expression]);

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, [executionResult])!;
        }
    }
}
