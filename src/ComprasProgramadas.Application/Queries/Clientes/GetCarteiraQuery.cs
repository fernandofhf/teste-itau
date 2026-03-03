using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Queries.Clientes;

public record GetCarteiraQuery(long ClienteId) : IRequest<CarteiraResponse>;

public class GetCarteiraHandler : IRequestHandler<GetCarteiraQuery, CarteiraResponse>
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IContaGraficaRepository _contaRepo;
    private readonly ICustodiaRepository _custodiaRepo;
    private readonly ICotacaoRepository _cotacaoRepo;

    public GetCarteiraHandler(
        IClienteRepository clienteRepo,
        IContaGraficaRepository contaRepo,
        ICustodiaRepository custodiaRepo,
        ICotacaoRepository cotacaoRepo)
    {
        _clienteRepo = clienteRepo;
        _contaRepo = contaRepo;
        _custodiaRepo = custodiaRepo;
        _cotacaoRepo = cotacaoRepo;
    }

    public async Task<CarteiraResponse> Handle(GetCarteiraQuery request, CancellationToken ct)
    {
        var cliente = await _clienteRepo.ObterPorIdAsync(request.ClienteId, ct)
            ?? throw new KeyNotFoundException("Cliente não encontrado.");

        var conta = await _contaRepo.ObterFilhotePorClienteIdAsync(cliente.Id, ct)
            ?? throw new InvalidOperationException("Conta gráfica não encontrada.");

        var custodias = (await _custodiaRepo.ObterPorContaAsync(conta.Id, ct)).ToList();

        if (!custodias.Any())
            return new CarteiraResponse(cliente.Id, cliente.Nome, conta.NumeroConta, DateTime.UtcNow,
                new ResumoCarteiraDto(0, 0, 0, 0), Enumerable.Empty<AtivoCarteiraDto>());

        var tickers = custodias.Select(c => c.Ticker).ToList();
        var cotacoes = (await _cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(tickers, ct))
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        var ativos = custodias.Select(c =>
        {
            var cotacaoAtual = cotacoes.GetValueOrDefault(c.Ticker, c.PrecoMedio);
            var valorAtual = c.Quantidade * cotacaoAtual;
            var pl = c.CalcularPL(cotacaoAtual);
            var plPercentual = c.PrecoMedio > 0 ? (cotacaoAtual - c.PrecoMedio) / c.PrecoMedio * 100 : 0;

            return new AtivoCarteiraDto(c.Ticker, c.Quantidade, c.PrecoMedio, cotacaoAtual, valorAtual, pl, plPercentual, 0);
        }).ToList();

        var valorTotalInvestido = ativos.Sum(a => a.PrecoMedio * a.Quantidade);
        var valorAtualTotal = ativos.Sum(a => a.ValorAtual);
        var plTotal = valorAtualTotal - valorTotalInvestido;
        var rentabilidade = valorTotalInvestido > 0 ? plTotal / valorTotalInvestido * 100 : 0;

        var ativosComComposicao = ativos.Select(a => a with
        {
            ComposicaoCarteira = valorAtualTotal > 0 ? a.ValorAtual / valorAtualTotal * 100 : 0
        }).ToList();

        return new CarteiraResponse(
            cliente.Id, cliente.Nome, conta.NumeroConta, DateTime.UtcNow,
            new ResumoCarteiraDto(
                Math.Round(valorTotalInvestido, 2),
                Math.Round(valorAtualTotal, 2),
                Math.Round(plTotal, 2),
                Math.Round(rentabilidade, 2)),
            ativosComComposicao);
    }
}
