using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IHistoricoOrdemClienteRepository
{
    Task AdicionarAsync(HistoricoOrdemCliente ordem, CancellationToken ct = default);
    Task AdicionarRangeAsync(IEnumerable<HistoricoOrdemCliente> ordens, CancellationToken ct = default);
    Task<IEnumerable<HistoricoOrdemCliente>> ObterPorClienteAsync(long clienteId, CancellationToken ct = default);
}
