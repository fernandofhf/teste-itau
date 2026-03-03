using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class CestaRecomendacaoRepository : ICestaRecomendacaoRepository
{
    private readonly AppDbContext _context;

    public CestaRecomendacaoRepository(AppDbContext context) => _context = context;

    public async Task<CestaRecomendacao?> ObterAtivaAsync(CancellationToken ct = default)
        => await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa, ct);

    public async Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync(CancellationToken ct = default)
        => await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync(ct);

    public async Task AdicionarAsync(CestaRecomendacao cesta, CancellationToken ct = default)
    {
        await _context.CestasRecomendacao.AddAsync(cesta, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(CestaRecomendacao cesta, CancellationToken ct = default)
    {
        _context.CestasRecomendacao.Update(cesta);
        await _context.SaveChangesAsync(ct);
    }
}
