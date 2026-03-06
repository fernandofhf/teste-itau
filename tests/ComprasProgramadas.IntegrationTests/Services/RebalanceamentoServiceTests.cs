using ComprasProgramadas.Application.Commands.Clientes;
using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Application.Services.Interfaces;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using ComprasProgramadas.Infrastructure.Persistence.Repositories;
using ComprasProgramadas.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ComprasProgramadas.IntegrationTests.Services;

public class RebalanceamentoServiceTests
{
    private static RebalanceamentoService CriarService(
        AppDbContext context,
        ICotahistService? cotahistService = null,
        IKafkaProducer? kafkaProducer = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CotacoesPath"] = "cotacoes" })
            .Build();

        return new RebalanceamentoService(
            new ClienteRepository(context),
            new ContaGraficaRepository(context),
            new CustodiaRepository(context),
            new EventoIRRepository(context),
            new RebalanceamentoRepository(context),
            new CotacaoRepository(context),
            cotahistService ?? Mock.Of<ICotahistService>(),
            kafkaProducer ?? Mock.Of<IKafkaProducer>(),
            new HistoricoOrdemClienteRepository(context),
            config,
            Mock.Of<ILogger<RebalanceamentoService>>());
    }

    [Fact]
    public async Task ExecutarRebalanceamento_SemClientesAtivos_RetornaZeroRebalanceados()
    {
        using var context = TestDbContextFactory.Create();
        var service = CriarService(context);

        var cestaAntiga = new CestaRecomendacao("Antiga", new[] { new ItemCesta("PETR4", 100m) });
        var cestaNova = new CestaRecomendacao("Nova", new[] { new ItemCesta("VALE3", 100m) });

        var result = await service.ExecutarRebalanceamentoPorMudancaCestaAsync(
            cestaAntiga, cestaNova, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalClientesRebalanceados.Should().Be(0);
        result.Mensagem.Should().Contain("rebalanceamento");
    }

    [Fact]
    public async Task ExecutarRebalanceamento_TickerSaiu_VendeECriaRegistroRebalanceamento()
    {
        using var context = TestDbContextFactory.Create();

        // Criar cliente com conta filhote
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Maria", "98765432100", "m@test.com", 600m),
            CancellationToken.None);

        // Criar custódia filhote: 100 PETR4 a R$30 + 50 VALE3 a R$25
        var custodiaRepo = new CustodiaRepository(context);
        var contaFilhote = await contaRepo.ObterFilhotePorClienteIdAsync(adesao.ClienteId);

        var custodiaP4 = new Custodia(contaFilhote!.Id, "PETR4");
        custodiaP4.AdicionarAtivos(100, 30m);
        await custodiaRepo.AdicionarAsync(custodiaP4);

        var custodiaV3 = new Custodia(contaFilhote.Id, "VALE3");
        custodiaV3.AdicionarAtivos(50, 25m);
        await custodiaRepo.AdicionarAsync(custodiaV3);

        // Cestas: antiga com PETR4+VALE3, nova com apenas VALE3+MGLU3
        var cestaAntiga = new CestaRecomendacao("Antiga",
            new[] { new ItemCesta("PETR4", 50m), new ItemCesta("VALE3", 50m) });
        var cestaNova = new CestaRecomendacao("Nova",
            new[] { new ItemCesta("VALE3", 60m), new ItemCesta("MGLU3", 40m) });

        // Cotacoes: PETR4=35, VALE3=28, MGLU3=20
        var cotahistMock = new Mock<ICotahistService>();
        cotahistMock
            .Setup(s => s.ObterCotacoesPorTickers(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[]
            {
                new Cotacao(DateOnly.FromDateTime(DateTime.UtcNow), "PETR4", 34m, 35m, 36m, 33m),
                new Cotacao(DateOnly.FromDateTime(DateTime.UtcNow), "VALE3", 27m, 28m, 29m, 26m),
                new Cotacao(DateOnly.FromDateTime(DateTime.UtcNow), "MGLU3", 19m, 20m, 21m, 18m),
            });

        var kafkaMock = new Mock<IKafkaProducer>();
        kafkaMock
            .Setup(k => k.PublicarAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CriarService(context, cotahistMock.Object, kafkaMock.Object);

        var result = await service.ExecutarRebalanceamentoPorMudancaCestaAsync(
            cestaAntiga, cestaNova, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalClientesRebalanceados.Should().Be(1);

        // Verificar que PETR4 foi removida da custódia filhote
        var custP4Depois = await custodiaRepo.ObterPorContaETickerAsync(contaFilhote.Id, "PETR4");
        custP4Depois!.Quantidade.Should().Be(0);

        // Verificar que MGLU3 foi comprada
        var custMglu = await custodiaRepo.ObterPorContaETickerAsync(contaFilhote.Id, "MGLU3");
        custMglu.Should().NotBeNull();
        custMglu!.Quantidade.Should().BeGreaterThan(0);

        // Verificar que EventoIR foi gerado (vendas < 20k → isento)
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var eventoIRRepo = new EventoIRRepository(context);
        var totalVendas = await eventoIRRepo.ObterTotalVendasMesAsync(
            adesao.ClienteId, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        // EventoIR de venda foi criado mas com valorBase = 3500 (abaixo de 20k, isento)
        totalVendas.Should().Be(3500m); // 100 * R$35
    }

    [Fact]
    public async Task ExecutarRebalanceamento_SemCotacoesDisponiveis_UsaPrecoMedioCustodia()
    {
        using var context = TestDbContextFactory.Create();

        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Carlos", "11122244455", "c@test.com", 600m),
            CancellationToken.None);

        var custodiaRepo = new CustodiaRepository(context);
        var contaFilhote = await contaRepo.ObterFilhotePorClienteIdAsync(adesao.ClienteId);

        var custodiaP4 = new Custodia(contaFilhote!.Id, "PETR4");
        custodiaP4.AdicionarAtivos(10, 30m);
        await custodiaRepo.AdicionarAsync(custodiaP4);

        var cestaAntiga = new CestaRecomendacao("Antiga", new[] { new ItemCesta("PETR4", 100m) });
        var cestaNova = new CestaRecomendacao("Nova", new[] { new ItemCesta("VALE3", 100m) });

        // Sem cotações disponíveis
        var cotahistMock = new Mock<ICotahistService>();
        cotahistMock
            .Setup(s => s.ObterCotacoesPorTickers(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Enumerable.Empty<Cotacao>());

        var service = CriarService(context, cotahistMock.Object);

        // Deve usar preço médio da custódia como cotação fallback
        var result = await service.ExecutarRebalanceamentoPorMudancaCestaAsync(
            cestaAntiga, cestaNova, CancellationToken.None);

        result.TotalClientesRebalanceados.Should().Be(1);

        // PETR4 removida (cotação fallback = preço médio = 30)
        var custP4 = await custodiaRepo.ObterPorContaETickerAsync(contaFilhote.Id, "PETR4");
        custP4!.Quantidade.Should().Be(0);
    }

    [Fact]
    public async Task ExecutarRebalanceamento_KafkaFalha_NaoInterrompeProcesso()
    {
        using var context = TestDbContextFactory.Create();

        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Luiza", "99988877766", "l@test.com", 600m),
            CancellationToken.None);

        var custodiaRepo = new CustodiaRepository(context);
        var contaFilhote = await contaRepo.ObterFilhotePorClienteIdAsync(adesao.ClienteId);

        var custodiaP4 = new Custodia(contaFilhote!.Id, "PETR4");
        custodiaP4.AdicionarAtivos(10, 30m);
        await custodiaRepo.AdicionarAsync(custodiaP4);

        var cestaAntiga = new CestaRecomendacao("Antiga", new[] { new ItemCesta("PETR4", 100m) });
        var cestaNova = new CestaRecomendacao("Nova", new[] { new ItemCesta("VALE3", 100m) });

        var cotahistMock = new Mock<ICotahistService>();
        cotahistMock
            .Setup(s => s.ObterCotacoesPorTickers(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Enumerable.Empty<Cotacao>());

        // Kafka que lança exceção
        var kafkaMock = new Mock<IKafkaProducer>();
        kafkaMock
            .Setup(k => k.PublicarAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Kafka indisponível"));

        var service = CriarService(context, cotahistMock.Object, kafkaMock.Object);

        // Não deve lançar exceção mesmo com Kafka falhando
        var act = async () => await service.ExecutarRebalanceamentoPorMudancaCestaAsync(
            cestaAntiga, cestaNova, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
