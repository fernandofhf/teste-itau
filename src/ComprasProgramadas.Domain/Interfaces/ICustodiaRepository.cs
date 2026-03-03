using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface ICustodiaRepository
{
    Task<Custodia?> ObterPorContaETickerAsync(long contaGraficaId, string ticker, CancellationToken ct = default);
    Task<IEnumerable<Custodia>> ObterPorContaAsync(long contaGraficaId, CancellationToken ct = default);
    Task<IEnumerable<Custodia>> ObterCustodiaMasterAsync(CancellationToken ct = default);
    Task AdicionarAsync(Custodia custodia, CancellationToken ct = default);
    Task AtualizarAsync(Custodia custodia, CancellationToken ct = default);
}
