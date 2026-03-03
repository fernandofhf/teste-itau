using ComprasProgramadas.Application.DTOs;

namespace ComprasProgramadas.Application.Services;

public interface IMotorCompraService
{
    Task<ExecutarCompraResponse> ExecutarCompraAsync(DateOnly dataReferencia, CancellationToken ct = default);
    bool IsDataCompra(DateOnly data);
    DateOnly ObterProximaDataCompra(DateOnly dataAtual);
}
