using Domain.Models;

namespace Application.Interfaces;

/// <summary>
/// Contrato para armazenamento e recuperação de tarefas e requisições de IA.
/// </summary>
public interface IAiTaskStore
{
    void Save(AiTaskResponse response);
    AiTaskResponse? Get(Guid requestId);
    IReadOnlyList<AiTaskResponse> GetAll(int limit = 50);
}
