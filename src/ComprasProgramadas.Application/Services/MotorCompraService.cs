using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Services.Interfaces;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComprasProgramadas.Application.Services;

public class MotorCompraService : IMotorCompraService
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IContaGraficaRepository _contaRepo;
    private readonly ICustodiaRepository _custodiaRepo;
    private readonly ICestaRecomendacaoRepository _cestaRepo;
    private readonly IOrdemCompraRepository _ordemRepo;
    private readonly IDistribuicaoRepository _distribuicaoRepo;
    private readonly IEventoIRRepository _eventoIRRepo;
    private readonly ICotacaoRepository _cotacaoRepo;
    private readonly ICotahistService _cotahistService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<MotorCompraService> _logger;
    private readonly string _pastaCotacoes;
    private const string TOPICO_IR = "ir-eventos";

    public MotorCompraService(
        IClienteRepository clienteRepo,
        IContaGraficaRepository contaRepo,
        ICustodiaRepository custodiaRepo,
        ICestaRecomendacaoRepository cestaRepo,
        IOrdemCompraRepository ordemRepo,
        IDistribuicaoRepository distribuicaoRepo,
        IEventoIRRepository eventoIRRepo,
        ICotacaoRepository cotacaoRepo,
        ICotahistService cotahistService,
        IKafkaProducer kafkaProducer,
        IConfiguration configuration,
        ILogger<MotorCompraService> logger)
    {
        _clienteRepo = clienteRepo;
        _contaRepo = contaRepo;
        _custodiaRepo = custodiaRepo;
        _cestaRepo = cestaRepo;
        _ordemRepo = ordemRepo;
        _distribuicaoRepo = distribuicaoRepo;
        _eventoIRRepo = eventoIRRepo;
        _cotacaoRepo = cotacaoRepo;
        _cotahistService = cotahistService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
        _pastaCotacoes = configuration["CotacoesPath"] ?? "cotacoes";
    }

    public bool IsDataCompra(DateOnly data)
    {
        var diasAlvo = new[] { 5, 15, 25 };
        if (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday)
            return false;

        foreach (var dia in diasAlvo)
        {
            var dataAlvo = new DateOnly(data.Year, data.Month, dia);
            var dataUtil = CalcularProximoDiaUtil(dataAlvo);
            if (dataUtil == data) return true;
        }
        return false;
    }

    public DateOnly ObterProximaDataCompra(DateOnly dataAtual)
    {
        var diasAlvo = new[] { 5, 15, 25 };
        foreach (var dia in diasAlvo)
        {
            if (dia > dataAtual.Day)
            {
                var dataAlvo = new DateOnly(dataAtual.Year, dataAtual.Month, dia);
                return CalcularProximoDiaUtil(dataAlvo);
            }
        }
        var proximoMes = dataAtual.AddMonths(1);
        return CalcularProximoDiaUtil(new DateOnly(proximoMes.Year, proximoMes.Month, 5));
    }

    private static DateOnly CalcularProximoDiaUtil(DateOnly data)
    {
        while (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday)
            data = data.AddDays(1);
        return data;
    }

    public async Task<ExecutarCompraResponse> ExecutarCompraAsync(DateOnly dataReferencia, CancellationToken ct = default)
    {
        _logger.LogInformation("Iniciando motor de compra para data {Data}", dataReferencia);

        // Buscar clientes ativos
        var clientes = (await _clienteRepo.ObterAtivosAsync(ct)).ToList();
        if (!clientes.Any())
            return new ExecutarCompraResponse(DateTime.UtcNow, 0, 0,
                Enumerable.Empty<OrdemCompraDto>(), Enumerable.Empty<DistribuicaoClienteDto>(),
                Enumerable.Empty<ResiduoDto>(), 0, "Nenhum cliente ativo para compra.");

        // Calcular 1/3 de cada cliente
        var aportesPorCliente = clientes.ToDictionary(c => c.Id, c => c.CalcularValorParcela());
        var totalConsolidado = aportesPorCliente.Values.Sum();
        
        // Buscar cesta ativa
        var cesta = await _cestaRepo.ObterAtivaAsync(ct)
            ?? throw new InvalidOperationException("Nenhuma cesta ativa encontrada.");

        // Obter cotações do arquivo COTAHIST
        var tickersCesta = cesta.Itens.Select(i => i.Ticker).ToList();
        var cotacoesParsed = _cotahistService.ObterCotacoesPorTickers(_pastaCotacoes, tickersCesta).ToList();

        if (cotacoesParsed.Any())
            await _cotacaoRepo.AdicionarOuAtualizarAsync(cotacoesParsed, ct);

        var cotacoes = (await _cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(tickersCesta, ct))
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        // Verificar saldo custódia master
        var master = await _contaRepo.ObterMasterAsync(ct)
            ?? throw new InvalidOperationException("Conta master não encontrada.");
        var custodiasMaster = (await _custodiaRepo.ObterCustodiaMasterAsync(ct))
            .ToDictionary(c => c.Ticker, c => c);

        // Calcular quantidade a comprar por ativo
        var ordensParaExecutar = new List<(string Ticker, int QtdComprar, decimal Preco)>();
        foreach (var item in cesta.Itens)
        {
            if (!cotacoes.TryGetValue(item.Ticker, out var preco) || preco <= 0)
            {
                _logger.LogWarning("Cotação não encontrada para {Ticker}", item.Ticker);
                continue;
            }

            var valorCesta = totalConsolidado * (item.Percentual / 100m);
            var qtdBruta = (int)Math.Truncate(valorCesta / preco);
            var saldoMaster = custodiasMaster.TryGetValue(item.Ticker, out var cm) ? cm.Quantidade : 0;
            var qtdComprar = Math.Max(0, qtdBruta - saldoMaster);

            ordensParaExecutar.Add((item.Ticker, qtdComprar, preco));
        }

        // Separar lote padrão vs fracionário e registrar ordens
        var ordensDb = new List<OrdemCompra>();
        var ordensDto = new List<OrdemCompraDto>();

        foreach (var (ticker, qtd, preco) in ordensParaExecutar)
        {
            var detalhes = new List<DetalheOrdemDto>();
            var lotePadrao = (qtd / 100) * 100;
            var fracionario = qtd % 100;

            if (lotePadrao > 0)
            {
                ordensDb.Add(new OrdemCompra(master.Id, ticker, lotePadrao, preco, TipoMercado.LotePadrao));
                detalhes.Add(new DetalheOrdemDto("LOTE_PADRAO", ticker, lotePadrao));
            }
            if (fracionario > 0)
            {
                ordensDb.Add(new OrdemCompra(master.Id, ticker + "F", fracionario, preco, TipoMercado.Fracionario));
                detalhes.Add(new DetalheOrdemDto("FRACIONARIO", ticker + "F", fracionario));
            }

            ordensDto.Add(new OrdemCompraDto(ticker, qtd, detalhes, preco, qtd * preco));
        }

        if (ordensDb.Any())
            await _ordemRepo.AdicionarRangeAsync(ordensDb, ct);

        // Atualizar custódia master (+compras)
        var qtdDisponivel = new Dictionary<string, int>();
        foreach (var (ticker, qtdComprar, preco) in ordensParaExecutar)
        {
            var saldoMaster = custodiasMaster.TryGetValue(ticker, out var cm) ? cm.Quantidade : 0;
            var qtdTotal = qtdComprar + saldoMaster;
            qtdDisponivel[ticker] = qtdTotal;

            if (qtdComprar > 0)
            {
                if (cm != null)
                {
                    cm.AdicionarAtivos(qtdComprar, preco);
                    await _custodiaRepo.AtualizarAsync(cm, ct);
                }
                else
                {
                    // Pode existir com Quantidade=0 (distribuída anteriormente); não está no dicionário pois foi filtrada
                    var existente = await _custodiaRepo.ObterPorContaETickerAsync(master.Id, ticker, ct);
                    if (existente != null)
                    {
                        existente.AdicionarAtivos(qtdComprar, preco);
                        await _custodiaRepo.AtualizarAsync(existente, ct);
                    }
                    else
                    {
                        var novaCustodia = new Custodia(master.Id, ticker);
                        novaCustodia.AdicionarAtivos(qtdComprar, preco);
                        await _custodiaRepo.AdicionarAsync(novaCustodia, ct);
                    }
                }
            }
        }

        // Distribuir para cada cliente
        var distribuicoesDto = new List<DistribuicaoClienteDto>();
        var distribuicoesDb = new List<Distribuicao>();
        int eventosIRPublicados = 0;

        var distribuicaoAcumulada = new Dictionary<string, int>();
        var ordensLookup = ordensParaExecutar.ToDictionary(o => o.Ticker, o => o);

        foreach (var cliente in clientes)
        {
            var aporte = aportesPorCliente[cliente.Id];
            var proporcao = totalConsolidado > 0 ? aporte / totalConsolidado : 0;

            var contaFilhote = await _contaRepo.ObterFilhotePorClienteIdAsync(cliente.Id, ct);
            if (contaFilhote == null) continue;

            var ativosDistribuidos = new List<AtivoDistribuidoDto>();

            foreach (var (ticker, _, preco) in ordensParaExecutar)
            {
                if (!qtdDisponivel.TryGetValue(ticker, out var total) || total == 0) continue;

                var qtdCliente = (int)Math.Truncate(proporcao * total);
                if (qtdCliente == 0) continue;

                distribuicaoAcumulada.TryAdd(ticker, 0);
                distribuicaoAcumulada[ticker] += qtdCliente;

                // Atualizar custódia filhote
                var custodiaFilhote = await _custodiaRepo.ObterPorContaETickerAsync(contaFilhote.Id, ticker, ct);
                if (custodiaFilhote == null)
                {
                    custodiaFilhote = new Custodia(contaFilhote.Id, ticker);
                    await _custodiaRepo.AdicionarAsync(custodiaFilhote, ct);
                }
                custodiaFilhote.AdicionarAtivos(qtdCliente, preco);
                await _custodiaRepo.AtualizarAsync(custodiaFilhote, ct);

                var ordemPrincipal = ordensDb.FirstOrDefault(o => o.Ticker == ticker || o.Ticker == ticker + "F");
                if (ordemPrincipal != null)
                {
                    var dist = new Distribuicao(ordemPrincipal.Id, custodiaFilhote.Id, ticker, qtdCliente, preco);
                    distribuicoesDb.Add(dist);
                    ativosDistribuidos.Add(new AtivoDistribuidoDto(ticker, qtdCliente));

                    // Calcular IR dedo-duro
                    var irDedoDuro = dist.CalcularIRDedoDuro();
                    var eventoIR = new EventoIR(cliente.Id, TipoEventoIR.DedoDuro, dist.ValorOperacao, irDedoDuro);
                    await _eventoIRRepo.AdicionarAsync(eventoIR, ct);

                    try
                    {
                        await _kafkaProducer.PublicarAsync(TOPICO_IR, new
                        {
                            tipo = "IR_DEDO_DURO",
                            clienteId = cliente.Id,
                            cpf = cliente.CPF,
                            ticker,
                            tipoOperacao = "COMPRA",
                            quantidade = qtdCliente,
                            precoUnitario = preco,
                            valorOperacao = dist.ValorOperacao,
                            aliquota = 0.00005m,
                            valorIR = irDedoDuro,
                            dataOperacao = DateTime.UtcNow
                        }, ct);
                        eventoIR.MarcarPublicado();
                        await _eventoIRRepo.AtualizarAsync(eventoIR, ct);
                        eventosIRPublicados++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao publicar IR dedo-duro no Kafka para cliente {Id}", cliente.Id);
                    }
                }
            }

            distribuicoesDto.Add(new DistribuicaoClienteDto(cliente.Id, cliente.Nome, aporte, ativosDistribuidos));
        }

        if (distribuicoesDb.Any())
            await _distribuicaoRepo.AdicionarRangeAsync(distribuicoesDb, ct);

        // Passo 15-16: Descontar distribuídos e calcular resíduos
        var residuos = new List<ResiduoDto>();
        foreach (var (ticker, totalDistribuido) in distribuicaoAcumulada)
        {
            var total = qtdDisponivel.GetValueOrDefault(ticker, 0);
            var residuo = total - totalDistribuido;

            var custMaster = await _custodiaRepo.ObterPorContaETickerAsync(master.Id, ticker, ct);
            if (custMaster != null && totalDistribuido > 0)
            {
                custMaster.RemoverAtivos(Math.Min(totalDistribuido, custMaster.Quantidade));
                await _custodiaRepo.AtualizarAsync(custMaster, ct);
            }

            if (residuo > 0)
                residuos.Add(new ResiduoDto(ticker, residuo));
        }

        _logger.LogInformation("Motor de compra concluído. {TotalClientes} clientes, R$ {Total}", clientes.Count, totalConsolidado);

        return new ExecutarCompraResponse(
            DateTime.UtcNow, clientes.Count, totalConsolidado,
            ordensDto, distribuicoesDto, residuos, eventosIRPublicados,
            $"Compra programada executada com sucesso para {clientes.Count} clientes.");
    }
}
