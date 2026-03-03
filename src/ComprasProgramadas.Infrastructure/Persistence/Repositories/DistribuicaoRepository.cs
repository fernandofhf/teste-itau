using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class DistribuicaoRepository : IDistribuicaoRepository
{
    private readonly AppDbContext _context;

    public DistribuicaoRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Distribuicao>> ObterPorClienteAsync(long clienteId, CancellationToken ct = default)
    {
        var contaFilhote = await _context.ContasGraficas
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);

        if (contaFilhote == null) return Enumerable.Empty<Distribuicao>();

        var custodias = await _context.Custodias
            .Where(c => c.ContaGraficaId == contaFilhote.Id)
            .Select(c => c.Id)
            .ToListAsync(ct);

        return await _context.Distribuicoes
            .Where(d => custodias.Contains(d.CustodiaFilhoteId))
            .OrderByDescending(d => d.DataDistribuicao)
            .ToListAsync(ct);
    }

    public async Task AdicionarRangeAsync(IEnumerable<Distribuicao> distribuicoes, CancellationToken ct = default)
    {
        await _context.Distribuicoes.AddRangeAsync(distribuicoes, ct);
        await _context.SaveChangesAsync(ct);
    }
}
