using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken ct = default);
    Task<IEnumerable<Cliente>> ObterAtivosAsync(CancellationToken ct = default);
    Task AdicionarAsync(Cliente cliente, CancellationToken ct = default);
    Task AtualizarAsync(Cliente cliente, CancellationToken ct = default);
}
