using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class OrdemCompraRepository : IOrdemCompraRepository
{
    private readonly AppDbContext _context;

    public OrdemCompraRepository(AppDbContext context) => _context = context;

    public async Task<bool> ExisteParaDataAsync(DateOnly data, CancellationToken ct = default)
    {
        var inicio = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = data.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await _context.OrdensCompra.AnyAsync(o => o.DataExecucao >= inicio && o.DataExecucao <= fim, ct);
    }

    public async Task AdicionarAsync(OrdemCompra ordem, CancellationToken ct = default)
    {
        await _context.OrdensCompra.AddAsync(ordem, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AdicionarRangeAsync(IEnumerable<OrdemCompra> ordens, CancellationToken ct = default)
    {
        await _context.OrdensCompra.AddRangeAsync(ordens, ct);
        await _context.SaveChangesAsync(ct);
    }
}
