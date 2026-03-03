using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IOrdemCompraRepository
{
    Task<bool> ExisteParaDataAsync(DateOnly data, CancellationToken ct = default);
    Task AdicionarAsync(OrdemCompra ordem, CancellationToken ct = default);
    Task AdicionarRangeAsync(IEnumerable<OrdemCompra> ordens, CancellationToken ct = default);
}
