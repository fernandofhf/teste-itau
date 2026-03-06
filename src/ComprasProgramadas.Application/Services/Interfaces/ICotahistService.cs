using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Application.Services.Interfaces;

public interface ICotahistService
{
    IEnumerable<Cotacao> ObterCotacoesPorTickers(string pastaCotacoes, IEnumerable<string> tickers);
    Cotacao? ObterCotacaoFechamento(string pastaCotacoes, string ticker);
}
