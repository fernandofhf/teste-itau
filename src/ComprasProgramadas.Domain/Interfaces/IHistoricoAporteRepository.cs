using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IHistoricoAporteRepository
{
    Task AdicionarAsync(HistoricoAporte historico, CancellationToken ct = default);
    Task<IEnumerable<HistoricoAporte>> ObterPorClienteAsync(long clienteId, CancellationToken ct = default);
}
