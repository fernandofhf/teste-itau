using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IEventoIRRepository
{
    Task<decimal> ObterTotalVendasMesAsync(long clienteId, int ano, int mes, CancellationToken ct = default);
    Task AdicionarAsync(EventoIR evento, CancellationToken ct = default);
    Task AtualizarAsync(EventoIR evento, CancellationToken ct = default);
}
