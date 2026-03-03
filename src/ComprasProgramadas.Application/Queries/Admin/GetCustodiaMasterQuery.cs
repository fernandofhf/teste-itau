using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Queries.Admin;

public record GetCustodiaMasterQuery : IRequest<CustodiaMasterResponse>;

public class GetCustodiaMasterHandler : IRequestHandler<GetCustodiaMasterQuery, CustodiaMasterResponse>
{
    private readonly IContaGraficaRepository _contaRepo;
    private readonly ICustodiaRepository _custodiaRepo;
    private readonly ICotacaoRepository _cotacaoRepo;

    public GetCustodiaMasterHandler(
        IContaGraficaRepository contaRepo,
        ICustodiaRepository custodiaRepo,
        ICotacaoRepository cotacaoRepo)
    {
        _contaRepo = contaRepo;
        _custodiaRepo = custodiaRepo;
        _cotacaoRepo = cotacaoRepo;
    }

    public async Task<CustodiaMasterResponse> Handle(GetCustodiaMasterQuery request, CancellationToken ct)
    {
        var master = await _contaRepo.ObterMasterAsync(ct)
            ?? throw new InvalidOperationException("Conta master não encontrada.");

        var custodias = (await _custodiaRepo.ObterCustodiaMasterAsync(ct)).ToList();

        var tickers = custodias.Select(c => c.Ticker).ToList();
        var cotacoes = (await _cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(tickers, ct))
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        var itens = custodias.Select(c =>
        {
            var cotacaoAtual = cotacoes.GetValueOrDefault(c.Ticker, c.PrecoMedio);
            return new CustodiaItemDto(c.Ticker, c.Quantidade, c.PrecoMedio, cotacaoAtual * c.Quantidade);
        }).ToList();

        return new CustodiaMasterResponse(
            new ContaMasterDto(master.Id, master.NumeroConta, master.Tipo.ToString()),
            itens,
            itens.Sum(i => i.ValorAtual));
    }
}
