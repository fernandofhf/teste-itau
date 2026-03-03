using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IDistribuicaoRepository
{
    Task<IEnumerable<Distribuicao>> ObterPorClienteAsync(long clienteId, CancellationToken ct = default);
    Task AdicionarRangeAsync(IEnumerable<Distribuicao> distribuicoes, CancellationToken ct = default);
}
