using System.Collections.Concurrent;
using Application.Interfaces;
using Domain.Models;

namespace Application.Services;

public sealed class InMemoryAiTaskStore : IAiTaskStore
{
    private readonly ConcurrentDictionary<Guid, AiTaskResponse> _tasks = new();
    private readonly ConcurrentQueue<Guid> _orderQueue = new();
    private const int MaxEntries = 200;

    public void Save(AiTaskResponse response)
    {
        _tasks[response.RequestId] = response;
        _orderQueue.Enqueue(response.RequestId);

        // Limpeza de histórico antigo para evitar vazamento de memória
        while (_orderQueue.Count > MaxEntries && _orderQueue.TryDequeue(out var oldId))
        {
            _tasks.TryRemove(oldId, out _);
        }
    }

    public AiTaskResponse? Get(Guid requestId)
    {
        _tasks.TryGetValue(requestId, out var task);
        return task;
    }

    public IReadOnlyList<AiTaskResponse> GetAll(int limit = 50)
    {
        return _tasks.Values
            .OrderByDescending(t => t.CompletedAt)
            .Take(limit)
            .ToList()
            .AsReadOnly();
    }
}
