using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Infrastructure.Cotacoes;

public class CotahistService : ICotahistService
{
    private readonly CotahistParser _parser;

    public CotahistService(CotahistParser parser) => _parser = parser;

    public IEnumerable<Cotacao> ObterCotacoesPorTickers(string pastaCotacoes, IEnumerable<string> tickers)
        => _parser.ObterCotacoesPorTickers(pastaCotacoes, tickers);

    public Cotacao? ObterCotacaoFechamento(string pastaCotacoes, string ticker)
        => _parser.ObterCotacaoFechamento(pastaCotacoes, ticker);
}
