using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Queries.Clientes;

public record GetRentabilidadeQuery(long ClienteId) : IRequest<RentabilidadeResponse>;

public class GetRentabilidadeHandler : IRequestHandler<GetRentabilidadeQuery, RentabilidadeResponse>
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IContaGraficaRepository _contaRepo;
    private readonly ICustodiaRepository _custodiaRepo;
    private readonly ICotacaoRepository _cotacaoRepo;
    private readonly IDistribuicaoRepository _distribuicaoRepo;

    public GetRentabilidadeHandler(
        IClienteRepository clienteRepo,
        IContaGraficaRepository contaRepo,
        ICustodiaRepository custodiaRepo,
        ICotacaoRepository cotacaoRepo,
        IDistribuicaoRepository distribuicaoRepo)
    {
        _clienteRepo = clienteRepo;
        _contaRepo = contaRepo;
        _custodiaRepo = custodiaRepo;
        _cotacaoRepo = cotacaoRepo;
        _distribuicaoRepo = distribuicaoRepo;
    }

    public async Task<RentabilidadeResponse> Handle(GetRentabilidadeQuery request, CancellationToken ct)
    {
        var cliente = await _clienteRepo.ObterPorIdAsync(request.ClienteId, ct)
            ?? throw new KeyNotFoundException("Cliente não encontrado.");

        var conta = await _contaRepo.ObterFilhotePorClienteIdAsync(cliente.Id, ct)
            ?? throw new InvalidOperationException("Conta gráfica não encontrada.");

        var custodias = (await _custodiaRepo.ObterPorContaAsync(conta.Id, ct)).ToList();
        var distribuicoes = (await _distribuicaoRepo.ObterPorClienteAsync(cliente.Id, ct)).ToList();

        var tickers = custodias.Select(c => c.Ticker).ToList();
        var cotacoes = (await _cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(tickers, ct))
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        var valorTotalInvestido = custodias.Sum(c => c.PrecoMedio * c.Quantidade);
        var valorAtualTotal = custodias.Sum(c => c.Quantidade * cotacoes.GetValueOrDefault(c.Ticker, c.PrecoMedio));
        var plTotal = valorAtualTotal - valorTotalInvestido;
        var rentabilidade = valorTotalInvestido > 0 ? plTotal / valorTotalInvestido * 100 : 0;

        var historicoAportes = distribuicoes
            .GroupBy(d => d.DataDistribuicao.Date)
            .OrderBy(g => g.Key)
            .Select((g, i) => new AporteHistoricoDto(
                g.Key,
                g.Sum(d => d.Quantidade * d.PrecoUnitario),
                $"{(i % 3) + 1}/3"))
            .ToList();

        var evolucaoCarteira = historicoAportes
            .Select(a => new EvolucaoCarteiraDto(
                a.Data,
                valorAtualTotal,
                valorTotalInvestido,
                Math.Round(rentabilidade, 2)))
            .ToList();

        return new RentabilidadeResponse(
            cliente.Id, cliente.Nome, DateTime.UtcNow,
            new RentabilidadeSummaryDto(
                Math.Round(valorTotalInvestido, 2),
                Math.Round(valorAtualTotal, 2),
                Math.Round(plTotal, 2),
                Math.Round(rentabilidade, 2)),
            historicoAportes,
            evolucaoCarteira);
    }
}
