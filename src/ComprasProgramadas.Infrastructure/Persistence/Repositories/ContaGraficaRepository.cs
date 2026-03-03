using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class ContaGraficaRepository : IContaGraficaRepository
{
    private readonly AppDbContext _context;

    public ContaGraficaRepository(AppDbContext context) => _context = context;

    public async Task<ContaGrafica?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await _context.ContasGraficas.FindAsync(new object[] { id }, ct);

    public async Task<ContaGrafica?> ObterMasterAsync(CancellationToken ct = default)
        => await _context.ContasGraficas.FirstOrDefaultAsync(c => c.Tipo == TipoConta.Master, ct);

    public async Task<ContaGrafica?> ObterFilhotePorClienteIdAsync(long clienteId, CancellationToken ct = default)
        => await _context.ContasGraficas
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Tipo == TipoConta.Filhote, ct);

    public async Task AdicionarAsync(ContaGrafica conta, CancellationToken ct = default)
    {
        await _context.ContasGraficas.AddAsync(conta, ct);
        await _context.SaveChangesAsync(ct);
    }
}
