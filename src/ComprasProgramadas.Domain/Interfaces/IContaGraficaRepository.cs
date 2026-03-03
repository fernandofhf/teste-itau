using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IContaGraficaRepository
{
    Task<ContaGrafica?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<ContaGrafica?> ObterMasterAsync(CancellationToken ct = default);
    Task<ContaGrafica?> ObterFilhotePorClienteIdAsync(long clienteId, CancellationToken ct = default);
    Task AdicionarAsync(ContaGrafica conta, CancellationToken ct = default);
}
