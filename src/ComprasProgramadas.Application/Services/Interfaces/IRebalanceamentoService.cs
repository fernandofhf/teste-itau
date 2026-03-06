using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Application.Services.Interfaces;

public interface IRebalanceamentoService
{
    Task<ExecutarRebalanceamentoResponse> ExecutarRebalanceamentoPorMudancaCestaAsync(
        CestaRecomendacao cestaAntiga,
        CestaRecomendacao cestaNova,
        CancellationToken ct = default);

    // RN-050/051/052: Rebalanceamento por desvio de proporção (limiar sugerido: 5pp)
    Task<ExecutarRebalanceamentoResponse> ExecutarRebalanceamentoPorDesvioAsync(
        CestaRecomendacao cesta,
        decimal limiarDesvioPercentual = 5m,
        CancellationToken ct = default);
}
