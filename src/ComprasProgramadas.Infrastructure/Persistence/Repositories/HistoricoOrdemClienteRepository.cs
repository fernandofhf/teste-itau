using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class HistoricoOrdemClienteRepository : IHistoricoOrdemClienteRepository
{
    private readonly AppDbContext _context;

    public HistoricoOrdemClienteRepository(AppDbContext context) => _context = context;

    public async Task AdicionarAsync(HistoricoOrdemCliente ordem, CancellationToken ct = default)
    {
        await _context.HistoricoOrdensCliente.AddAsync(ordem, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AdicionarRangeAsync(IEnumerable<HistoricoOrdemCliente> ordens, CancellationToken ct = default)
    {
        await _context.HistoricoOrdensCliente.AddRangeAsync(ordens, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<HistoricoOrdemCliente>> ObterPorClienteAsync(long clienteId, CancellationToken ct = default)
        => await _context.HistoricoOrdensCliente
            .Where(o => o.ClienteId == clienteId)
            .OrderByDescending(o => o.DataOperacao)
            .ToListAsync(ct);
}
