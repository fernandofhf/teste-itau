using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class RebalanceamentoRepository : IRebalanceamentoRepository
{
    private readonly AppDbContext _context;

    public RebalanceamentoRepository(AppDbContext context) => _context = context;

    public async Task AdicionarRangeAsync(IEnumerable<Rebalanceamento> rebalanceamentos, CancellationToken ct = default)
    {
        await _context.Rebalanceamentos.AddRangeAsync(rebalanceamentos, ct);
        await _context.SaveChangesAsync(ct);
    }
}
