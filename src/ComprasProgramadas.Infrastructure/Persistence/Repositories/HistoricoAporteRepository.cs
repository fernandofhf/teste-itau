using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class HistoricoAporteRepository : IHistoricoAporteRepository
{
    private readonly AppDbContext _context;

    public HistoricoAporteRepository(AppDbContext context) => _context = context;

    public async Task AdicionarAsync(HistoricoAporte historico, CancellationToken ct = default)
    {
        await _context.HistoricoAportes.AddAsync(historico, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<HistoricoAporte>> ObterPorClienteAsync(long clienteId, CancellationToken ct = default)
        => await _context.HistoricoAportes
            .Where(h => h.ClienteId == clienteId)
            .OrderByDescending(h => h.DataAlteracao)
            .ToListAsync(ct);
}
