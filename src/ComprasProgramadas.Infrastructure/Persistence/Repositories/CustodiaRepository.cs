using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class CustodiaRepository : ICustodiaRepository
{
    private readonly AppDbContext _context;

    public CustodiaRepository(AppDbContext context) => _context = context;

    public async Task<Custodia?> ObterPorContaETickerAsync(long contaGraficaId, string ticker, CancellationToken ct = default)
        => await _context.Custodias
            .FirstOrDefaultAsync(c => c.ContaGraficaId == contaGraficaId && c.Ticker == ticker, ct);

    public async Task<IEnumerable<Custodia>> ObterPorContaAsync(long contaGraficaId, CancellationToken ct = default)
        => await _context.Custodias
            .Where(c => c.ContaGraficaId == contaGraficaId && c.Quantidade > 0)
            .ToListAsync(ct);

    public async Task<IEnumerable<Custodia>> ObterCustodiaMasterAsync(CancellationToken ct = default)
    {
        var master = await _context.ContasGraficas
            .FirstOrDefaultAsync(c => c.Tipo == TipoConta.Master, ct);

        if (master == null) return Enumerable.Empty<Custodia>();

        return await _context.Custodias
            .Where(c => c.ContaGraficaId == master.Id && c.Quantidade > 0)
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(Custodia custodia, CancellationToken ct = default)
    {
        await _context.Custodias.AddAsync(custodia, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(Custodia custodia, CancellationToken ct = default)
    {
        _context.Custodias.Update(custodia);
        await _context.SaveChangesAsync(ct);
    }
}
