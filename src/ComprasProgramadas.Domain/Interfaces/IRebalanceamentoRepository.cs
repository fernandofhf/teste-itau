using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IRebalanceamentoRepository
{
    Task AdicionarRangeAsync(IEnumerable<Rebalanceamento> rebalanceamentos, CancellationToken ct = default);
}
