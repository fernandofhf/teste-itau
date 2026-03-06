using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Services.Interfaces;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComprasProgramadas.Application.Services;

public class RebalanceamentoService : IRebalanceamentoService
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IContaGraficaRepository _contaRepo;
    private readonly ICustodiaRepository _custodiaRepo;
    private readonly IEventoIRRepository _eventoIRRepo;
    private readonly IRebalanceamentoRepository _rebalanceamentoRepo;
    private readonly ICotacaoRepository _cotacaoRepo;
    private readonly ICotahistService _cotahistService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<RebalanceamentoService> _logger;
    private readonly string _pastaCotacoes;
    private const string TOPICO_IR = "ir-eventos";
    private const decimal LIMITE_ISENCAO_IR = 20_000m;

    public RebalanceamentoService(
        IClienteRepository clienteRepo,
        IContaGraficaRepository contaRepo,
        ICustodiaRepository custodiaRepo,
        IEventoIRRepository eventoIRRepo,
        IRebalanceamentoRepository rebalanceamentoRepo,
        ICotacaoRepository cotacaoRepo,
        ICotahistService cotahistService,
        IKafkaProducer kafkaProducer,
        IConfiguration configuration,
        ILogger<RebalanceamentoService> logger)
    {
        _clienteRepo = clienteRepo;
        _contaRepo = contaRepo;
        _custodiaRepo = custodiaRepo;
        _eventoIRRepo = eventoIRRepo;
        _rebalanceamentoRepo = rebalanceamentoRepo;
        _cotacaoRepo = cotacaoRepo;
        _cotahistService = cotahistService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
        _pastaCotacoes = configuration["CotacoesPath"] ?? "cotacoes";
    }

    // RN-040 a RN-048: Rebalanceamento por mudança de cesta
    public async Task<ExecutarRebalanceamentoResponse> ExecutarRebalanceamentoPorMudancaCestaAsync(
        CestaRecomendacao cestaAntiga,
        CestaRecomendacao cestaNova,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Iniciando rebalanceamento por mudança de cesta");

        var clientes = (await _clienteRepo.ObterAtivosAsync(ct)).ToList();
        if (!clientes.Any())
            return new ExecutarRebalanceamentoResponse(0, "Nenhum cliente ativo para rebalanceamento.");

        var tickersAntigos = cestaAntiga.Itens.Select(i => i.Ticker).ToHashSet();
        var tickersNovos = cestaNova.Itens.Select(i => i.Ticker).ToHashSet();
        var tickersSairam = tickersAntigos.Except(tickersNovos).ToList();
        var tickersEntraram = tickersNovos.Except(tickersAntigos).ToList();
        var todosTickersNecessarios = tickersAntigos.Union(tickersNovos).ToList();

        // Carregar cotações do arquivo COTAHIST
        var cotacoesParsed = _cotahistService.ObterCotacoesPorTickers(_pastaCotacoes, todosTickersNecessarios).ToList();
        if (cotacoesParsed.Any())
            await _cotacaoRepo.AdicionarOuAtualizarAsync(cotacoesParsed, ct);

        var cotacoes = (await _cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(todosTickersNecessarios, ct))
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        int clientesRebalanceados = 0;

        foreach (var cliente in clientes)
        {
            try
            {
                await RebalancearPorMudancaClienteAsync(cliente, cestaAntiga, cestaNova, tickersSairam, tickersEntraram, cotacoes, ct);
                clientesRebalanceados++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rebalancear cliente {Id}", cliente.Id);
            }
        }

        return new ExecutarRebalanceamentoResponse(
            clientesRebalanceados,
            $"Rebalanceamento concluído para {clientesRebalanceados} clientes.");
    }

    // RN-050/051/052: Rebalanceamento por desvio de proporção
    public async Task<ExecutarRebalanceamentoResponse> ExecutarRebalanceamentoPorDesvioAsync(
        CestaRecomendacao cesta,
        decimal limiarDesvioPercentual = 5m,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Iniciando rebalanceamento por desvio de proporção (limiar: {Limiar}pp)", limiarDesvioPercentual);

        var clientes = (await _clienteRepo.ObterAtivosAsync(ct)).ToList();
        if (!clientes.Any())
            return new ExecutarRebalanceamentoResponse(0, "Nenhum cliente ativo para rebalanceamento.");

        var tickers = cesta.Itens.Select(i => i.Ticker).ToList();

        var cotacoesParsed = _cotahistService.ObterCotacoesPorTickers(_pastaCotacoes, tickers).ToList();
        if (cotacoesParsed.Any())
            await _cotacaoRepo.AdicionarOuAtualizarAsync(cotacoesParsed, ct);

        var cotacoes = (await _cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(tickers, ct))
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        int clientesRebalanceados = 0;

        foreach (var cliente in clientes)
        {
            try
            {
                var rebalanceou = await RebalancearPorDesvioClienteAsync(cliente, cesta, cotacoes, limiarDesvioPercentual, ct);
                if (rebalanceou) clientesRebalanceados++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rebalancear por desvio cliente {Id}", cliente.Id);
            }
        }

        return new ExecutarRebalanceamentoResponse(
            clientesRebalanceados,
            $"Rebalanceamento por desvio concluído. {clientesRebalanceados} cliente(s) ajustados.");
    }

    private async Task RebalancearPorMudancaClienteAsync(
        Cliente cliente,
        CestaRecomendacao cestaAntiga,
        CestaRecomendacao cestaNova,
        List<string> tickersSairam,
        List<string> tickersEntraram,
        Dictionary<string, decimal> cotacoes,
        CancellationToken ct)
    {
        var contaFilhote = await _contaRepo.ObterFilhotePorClienteIdAsync(cliente.Id, ct);
        if (contaFilhote == null) return;

        var custodias = (await _custodiaRepo.ObterPorContaAsync(contaFilhote.Id, ct))
            .ToDictionary(c => c.Ticker, c => c);

        decimal totalValorVendas = 0;
        decimal lucroLiquido = 0;
        var rebalanceamentos = new List<Rebalanceamento>();

        // Passo 1: Vender ativos que saíram da cesta (RN-040)
        foreach (var ticker in tickersSairam)
        {
            if (!custodias.TryGetValue(ticker, out var custodia) || custodia.Quantidade == 0) continue;

            var cotacao = cotacoes.GetValueOrDefault(ticker, custodia.PrecoMedio);
            var valorVenda = custodia.Quantidade * cotacao;
            var lucroAtivo = custodia.CalcularPL(cotacao);

            totalValorVendas += valorVenda;
            lucroLiquido += lucroAtivo;

            custodia.RemoverAtivos(custodia.Quantidade);
            await _custodiaRepo.AtualizarAsync(custodia, ct);

            rebalanceamentos.Add(new Rebalanceamento(cliente.Id, TipoRebalanceamento.MudancaCesta, ticker, null, valorVenda));
        }

        // Passo 2: Ajustar ativos mantidos com % alterado (RN-041/RN-049)
        var tickersMantidos = cestaAntiga.Itens
            .Select(i => i.Ticker)
            .Intersect(cestaNova.Itens.Select(i => i.Ticker))
            .ToList();

        var valorTotalCarteira = custodias
            .Where(c => c.Value.Quantidade > 0)
            .Sum(c => c.Value.Quantidade * cotacoes.GetValueOrDefault(c.Key, c.Value.PrecoMedio));

        // Tickers mantidos mas sub-alocados (precisam receber mais): RN-049
        var alvosCompra = new Dictionary<string, decimal>(); // ticker → percentual na nova cesta

        foreach (var ticker in tickersMantidos)
        {
            if (!custodias.TryGetValue(ticker, out var custodia) || custodia.Quantidade == 0)
            {
                // Não possui posição — tratar como se tivesse entrado (RN-049)
                var percNovoCestaItem = cestaNova.Itens.FirstOrDefault(i => i.Ticker == ticker);
                if (percNovoCestaItem != null)
                    alvosCompra[ticker] = percNovoCestaItem.Percentual;
                continue;
            }

            var percentualNovo = cestaNova.Itens.First(i => i.Ticker == ticker).Percentual / 100m;
            var cotacao = cotacoes.GetValueOrDefault(ticker, custodia.PrecoMedio);
            var valorAlvo = (valorTotalCarteira + totalValorVendas) * percentualNovo;
            var valorAtual = custodia.Quantidade * cotacao;

            if (valorAtual > valorAlvo * 1.01m && cotacao > 0)
            {
                // Over-alocado: vender excesso
                var qtdVender = (int)Math.Truncate((valorAtual - valorAlvo) / cotacao);
                if (qtdVender > 0)
                {
                    var valorV = qtdVender * cotacao;
                    var lucroV = qtdVender * (cotacao - custodia.PrecoMedio);
                    totalValorVendas += valorV;
                    lucroLiquido += lucroV;
                    custodia.RemoverAtivos(qtdVender);
                    await _custodiaRepo.AtualizarAsync(custodia, ct);
                    rebalanceamentos.Add(new Rebalanceamento(cliente.Id, TipoRebalanceamento.MudancaCesta, ticker, null, valorV));
                }
            }
            else if (valorAtual < valorAlvo * 0.99m)
            {
                // Sub-alocado: marcar para comprar déficit (RN-049)
                alvosCompra[ticker] = cestaNova.Itens.First(i => i.Ticker == ticker).Percentual;
            }
        }

        // Adicionar tickers que entraram à lista de compra
        foreach (var ticker in tickersEntraram)
        {
            var item = cestaNova.Itens.FirstOrDefault(i => i.Ticker == ticker);
            if (item != null)
                alvosCompra[ticker] = item.Percentual;
        }

        // Passo 3: Comprar ativos que entraram + sub-alocados (com o valor das vendas)
        if (totalValorVendas > 0 && alvosCompra.Any())
        {
            var totalPercentualAlvos = alvosCompra.Values.Sum();

            foreach (var (ticker, percentualAlvo) in alvosCompra)
            {
                var cotacao = cotacoes.GetValueOrDefault(ticker);
                if (cotacao <= 0) continue;

                var proporcao = totalPercentualAlvos > 0 ? percentualAlvo / totalPercentualAlvos : 0;
                var valorCompra = totalValorVendas * proporcao;
                var qtdComprar = (int)Math.Truncate(valorCompra / cotacao);

                if (qtdComprar > 0)
                {
                    var custodiaEntrada = await _custodiaRepo.ObterPorContaETickerAsync(contaFilhote.Id, ticker, ct);
                    if (custodiaEntrada == null)
                    {
                        custodiaEntrada = new Custodia(contaFilhote.Id, ticker);
                        await _custodiaRepo.AdicionarAsync(custodiaEntrada, ct);
                    }
                    custodiaEntrada.AdicionarAtivos(qtdComprar, cotacao);
                    await _custodiaRepo.AtualizarAsync(custodiaEntrada, ct);
                    rebalanceamentos.Add(new Rebalanceamento(cliente.Id, TipoRebalanceamento.MudancaCesta, null, ticker, 0));
                }
            }
        }

        if (rebalanceamentos.Any())
            await _rebalanceamentoRepo.AdicionarRangeAsync(rebalanceamentos, ct);

        // Calcular e publicar IR sobre vendas (RN-044 a RN-048)
        if (totalValorVendas > 0)
            await PublicarIRVendaAsync(cliente, totalValorVendas, lucroLiquido, ct);
    }

    // RN-050/051/052: Rebalancear cliente por desvio de proporção
    private async Task<bool> RebalancearPorDesvioClienteAsync(
        Cliente cliente,
        CestaRecomendacao cesta,
        Dictionary<string, decimal> cotacoes,
        decimal limiarDesvioPercentual,
        CancellationToken ct)
    {
        var contaFilhote = await _contaRepo.ObterFilhotePorClienteIdAsync(cliente.Id, ct);
        if (contaFilhote == null) return false;

        var custodias = (await _custodiaRepo.ObterPorContaAsync(contaFilhote.Id, ct))
            .ToDictionary(c => c.Ticker, c => c);

        if (!custodias.Any()) return false;

        var valorTotal = custodias
            .Sum(c => c.Value.Quantidade * cotacoes.GetValueOrDefault(c.Key, c.Value.PrecoMedio));

        if (valorTotal <= 0) return false;

        // RN-050: Calcular percentual atual de cada ativo e comparar com o alvo
        bool temDesvio = false;
        foreach (var item in cesta.Itens)
        {
            var valorAtivo = custodias.TryGetValue(item.Ticker, out var cust)
                ? cust.Quantidade * cotacoes.GetValueOrDefault(item.Ticker, cust.PrecoMedio)
                : 0m;
            var percentualAtual = valorTotal > 0 ? (valorAtivo / valorTotal) * 100m : 0m;
            var desvio = Math.Abs(percentualAtual - item.Percentual);
            if (desvio >= limiarDesvioPercentual)
            {
                temDesvio = true;
                break;
            }
        }

        if (!temDesvio) return false;

        // RN-051: Executar rebalanceamento — vender over-alocados, comprar sub-alocados
        decimal totalValorVendas = 0;
        decimal lucroLiquido = 0;
        var rebalanceamentos = new List<Rebalanceamento>();
        var alvosCompra = new Dictionary<string, decimal>(); // ticker → percentual alvo

        foreach (var item in cesta.Itens)
        {
            if (!custodias.TryGetValue(item.Ticker, out var custodia) || custodia.Quantidade == 0)
            {
                alvosCompra[item.Ticker] = item.Percentual;
                continue;
            }

            var cotacao = cotacoes.GetValueOrDefault(item.Ticker, custodia.PrecoMedio);
            var valorAtual = custodia.Quantidade * cotacao;
            var valorAlvo = valorTotal * (item.Percentual / 100m);
            var percentualAtual = (valorAtual / valorTotal) * 100m;
            var desvio = percentualAtual - item.Percentual;

            if (desvio >= limiarDesvioPercentual && cotacao > 0)
            {
                // Over-alocado: vender excesso
                var qtdVender = (int)Math.Truncate((valorAtual - valorAlvo) / cotacao);
                if (qtdVender > 0)
                {
                    var valorV = qtdVender * cotacao;
                    var lucroV = qtdVender * (cotacao - custodia.PrecoMedio);
                    totalValorVendas += valorV;
                    lucroLiquido += lucroV;
                    custodia.RemoverAtivos(qtdVender);
                    await _custodiaRepo.AtualizarAsync(custodia, ct);
                    rebalanceamentos.Add(new Rebalanceamento(cliente.Id, TipoRebalanceamento.DesvioProporcao, item.Ticker, null, valorV));
                }
            }
            else if (desvio <= -limiarDesvioPercentual)
            {
                // Sub-alocado: marcar para compra
                alvosCompra[item.Ticker] = item.Percentual;
            }
        }

        // Comprar sub-alocados com o valor arrecadado nas vendas
        if (totalValorVendas > 0 && alvosCompra.Any())
        {
            var totalPercentualAlvos = alvosCompra.Values.Sum();

            foreach (var (ticker, percentualAlvo) in alvosCompra)
            {
                var cotacao = cotacoes.GetValueOrDefault(ticker);
                if (cotacao <= 0) continue;

                var proporcao = totalPercentualAlvos > 0 ? percentualAlvo / totalPercentualAlvos : 0;
                var valorCompra = totalValorVendas * proporcao;
                var qtdComprar = (int)Math.Truncate(valorCompra / cotacao);

                if (qtdComprar > 0)
                {
                    var custodiaEntrada = await _custodiaRepo.ObterPorContaETickerAsync(contaFilhote.Id, ticker, ct);
                    if (custodiaEntrada == null)
                    {
                        custodiaEntrada = new Custodia(contaFilhote.Id, ticker);
                        await _custodiaRepo.AdicionarAsync(custodiaEntrada, ct);
                    }
                    custodiaEntrada.AdicionarAtivos(qtdComprar, cotacao);
                    await _custodiaRepo.AtualizarAsync(custodiaEntrada, ct);
                    rebalanceamentos.Add(new Rebalanceamento(cliente.Id, TipoRebalanceamento.DesvioProporcao, null, ticker, 0));
                }
            }
        }

        if (rebalanceamentos.Any())
            await _rebalanceamentoRepo.AdicionarRangeAsync(rebalanceamentos, ct);

        // RN-052: Calcular IR sobre as vendas do rebalanceamento por desvio
        if (totalValorVendas > 0)
            await PublicarIRVendaAsync(cliente, totalValorVendas, lucroLiquido, ct);

        return rebalanceamentos.Any();
    }

    // Helper: calcular e publicar IR sobre vendas (reutilizado em ambos os tipos de rebalanceamento)
    private async Task PublicarIRVendaAsync(
        Cliente cliente,
        decimal totalValorVendas,
        decimal lucroLiquido,
        CancellationToken ct)
    {
        var totalVendasMes = await _eventoIRRepo.ObterTotalVendasMesAsync(
            cliente.Id, DateTime.UtcNow.Year, DateTime.UtcNow.Month, ct);
        var totalVendasComNovas = totalVendasMes + totalValorVendas;

        decimal valorIR = 0;
        if (totalVendasComNovas > LIMITE_ISENCAO_IR && lucroLiquido > 0)
            valorIR = Math.Round(lucroLiquido * 0.20m, 2);

        var eventoIR = new EventoIR(cliente.Id, TipoEventoIR.VendaAcoes, totalValorVendas, valorIR);
        await _eventoIRRepo.AdicionarAsync(eventoIR, ct);

        try
        {
            await _kafkaProducer.PublicarAsync(TOPICO_IR, new
            {
                tipo = "IR_VENDA",
                clienteId = cliente.Id,
                cpf = cliente.CPF,
                mesReferencia = $"{DateTime.UtcNow:yyyy-MM}",
                totalVendasMes = totalVendasComNovas,
                lucroLiquido,
                aliquota = totalVendasComNovas > LIMITE_ISENCAO_IR ? 0.20m : 0m,
                valorIR,
                dataCalculo = DateTime.UtcNow
            }, ct);
            eventoIR.MarcarPublicado();
            await _eventoIRRepo.AtualizarAsync(eventoIR, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar IR_VENDA no Kafka para cliente {Id}", cliente.Id);
        }
    }
}
