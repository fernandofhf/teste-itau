using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class CotacaoRepository : ICotacaoRepository
{
    private readonly AppDbContext _context;

    public CotacaoRepository(AppDbContext context) => _context = context;

    public async Task<Cotacao?> ObterUltimaCotacaoAsync(string ticker, CancellationToken ct = default)
        => await _context.Cotacoes
            .Where(c => c.Ticker == ticker.ToUpper())
            .OrderByDescending(c => c.DataPregao)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<Cotacao>> ObterUltimasCotacoesPorTickersAsync(IEnumerable<string> tickers, CancellationToken ct = default)
    {
        var tickersUpper = tickers.Select(t => t.ToUpper()).ToList();
        var ultimaData = await _context.Cotacoes
            .Where(c => tickersUpper.Contains(c.Ticker))
            .MaxAsync(c => (DateOnly?)c.DataPregao, ct);

        if (ultimaData == null) return Enumerable.Empty<Cotacao>();

        return await _context.Cotacoes
            .Where(c => tickersUpper.Contains(c.Ticker) && c.DataPregao == ultimaData)
            .ToListAsync(ct);
    }

    public async Task AdicionarOuAtualizarAsync(IEnumerable<Cotacao> cotacoes, CancellationToken ct = default)
    {
        foreach (var cotacao in cotacoes)
        {
            var existente = await _context.Cotacoes
                .FirstOrDefaultAsync(c => c.Ticker == cotacao.Ticker && c.DataPregao == cotacao.DataPregao, ct);

            if (existente == null)
                await _context.Cotacoes.AddAsync(cotacao, ct);
        }
        await _context.SaveChangesAsync(ct);
    }
}
