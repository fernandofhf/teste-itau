using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface ICestaRecomendacaoRepository
{
    Task<CestaRecomendacao?> ObterAtivaAsync(CancellationToken ct = default);
    Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync(CancellationToken ct = default);
    Task AdicionarAsync(CestaRecomendacao cesta, CancellationToken ct = default);
    Task AtualizarAsync(CestaRecomendacao cesta, CancellationToken ct = default);
}
