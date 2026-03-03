using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface ICotacaoRepository
{
    Task<Cotacao?> ObterUltimaCotacaoAsync(string ticker, CancellationToken ct = default);
    Task<IEnumerable<Cotacao>> ObterUltimasCotacoesPorTickersAsync(IEnumerable<string> tickers, CancellationToken ct = default);
    Task AdicionarOuAtualizarAsync(IEnumerable<Cotacao> cotacoes, CancellationToken ct = default);
}
