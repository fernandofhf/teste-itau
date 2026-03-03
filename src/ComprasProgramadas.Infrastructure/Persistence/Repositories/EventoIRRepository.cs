using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class EventoIRRepository : IEventoIRRepository
{
    private readonly AppDbContext _context;

    public EventoIRRepository(AppDbContext context) => _context = context;

    public async Task<decimal> ObterTotalVendasMesAsync(long clienteId, int ano, int mes, CancellationToken ct = default)
    {
        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);

        return await _context.EventosIR
            .Where(e => e.ClienteId == clienteId
                && e.Tipo == TipoEventoIR.VendaAcoes
                && e.DataEvento >= inicio
                && e.DataEvento < fim)
            .SumAsync(e => e.ValorBase, ct);
    }

    public async Task AdicionarAsync(EventoIR evento, CancellationToken ct = default)
    {
        await _context.EventosIR.AddAsync(evento, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(EventoIR evento, CancellationToken ct = default)
    {
        _context.EventosIR.Update(evento);
        await _context.SaveChangesAsync(ct);
    }
}
