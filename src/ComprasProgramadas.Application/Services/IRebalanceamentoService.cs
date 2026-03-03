using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Application.Services;

public interface IRebalanceamentoService
{
    Task<ExecutarRebalanceamentoResponse> ExecutarRebalanceamentoPorMudancaCestaAsync(
        CestaRecomendacao cestaAntiga,
        CestaRecomendacao cestaNova,
        CancellationToken ct = default);
}
