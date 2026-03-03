using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Queries.Admin;

public record GetCestaAtualQuery : IRequest<CestaResponse>;

public class GetCestaAtualHandler : IRequestHandler<GetCestaAtualQuery, CestaResponse>
{
    private readonly ICestaRecomendacaoRepository _cestaRepo;
    private readonly ICotacaoRepository _cotacaoRepo;

    public GetCestaAtualHandler(ICestaRecomendacaoRepository cestaRepo, ICotacaoRepository cotacaoRepo)
    {
        _cestaRepo = cestaRepo;
        _cotacaoRepo = cotacaoRepo;
    }

    public async Task<CestaResponse> Handle(GetCestaAtualQuery request, CancellationToken ct)
    {
        var cesta = await _cestaRepo.ObterAtivaAsync(ct)
            ?? throw new KeyNotFoundException("Nenhuma cesta ativa encontrada.");

        var tickers = cesta.Itens.Select(i => i.Ticker);
        var cotacoes = (await _cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(tickers, ct))
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        return new CestaResponse(
            cesta.Id, cesta.Nome, cesta.Ativa, cesta.DataCriacao, cesta.DataDesativacao,
            cesta.Itens.Select(i => new ItemCestaResponse(i.Ticker, i.Percentual,
                cotacoes.GetValueOrDefault(i.Ticker))),
            false, null, null, string.Empty);
    }
}
