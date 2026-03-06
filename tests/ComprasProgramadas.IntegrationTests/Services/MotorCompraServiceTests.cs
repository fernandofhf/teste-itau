using ComprasProgramadas.Application.Commands.Clientes;
using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Application.Services.Interfaces;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using ComprasProgramadas.Infrastructure.Persistence.Repositories;
using ComprasProgramadas.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ComprasProgramadas.IntegrationTests.Services;

public class MotorCompraServiceTests
{
    private static MotorCompraService CriarService(
        AppDbContext context,
        ICotahistService? cotahistService = null,
        IKafkaProducer? kafkaProducer = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CotacoesPath"] = "cotacoes" })
            .Build();

        return new MotorCompraService(
            new ClienteRepository(context),
            new ContaGraficaRepository(context),
            new CustodiaRepository(context),
            new CestaRecomendacaoRepository(context),
            new OrdemCompraRepository(context),
            new DistribuicaoRepository(context),
            new EventoIRRepository(context),
            new CotacaoRepository(context),
            cotahistService ?? Mock.Of<ICotahistService>(),
            kafkaProducer ?? Mock.Of<IKafkaProducer>(),
            new HistoricoOrdemClienteRepository(context),
            config,
            Mock.Of<ILogger<MotorCompraService>>());
    }

    [Fact]
    public async Task ExecutarCompraAsync_SemClientes_RetornaMensagemNenhumCliente()
    {
        using var context = TestDbContextFactory.Create();
        var service = CriarService(context);

        var result = await service.ExecutarCompraAsync(new DateOnly(2026, 3, 5));

        result.Should().NotBeNull();
        result.TotalClientes.Should().Be(0);
        result.Mensagem.Should().Contain("Nenhum cliente ativo");
    }

    [Fact]
    public async Task ExecutarCompraAsync_SemCestaAtiva_LancaInvalidOperationException()
    {
        using var context = TestDbContextFactory.Create();

        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        await adesaoHandler.Handle(
            new AderirProdutoCommand("João", "12345678901", "j@test.com", 300m),
            CancellationToken.None);

        var service = CriarService(context);

        var act = async () => await service.ExecutarCompraAsync(new DateOnly(2026, 3, 5));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cesta*");
    }

    [Fact]
    public async Task ExecutarCompraAsync_FluxoCompleto_DistribuiParaClienteERetornaSumario()
    {
        using var context = TestDbContextFactory.Create();

        // Criar conta master
        var contaRepo = new ContaGraficaRepository(context);
        var master = ContaGrafica.CriarMaster();
        await contaRepo.AdicionarAsync(master);

        // Criar cliente com adesão (cria conta filhote automaticamente)
        var clienteRepo = new ClienteRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        await adesaoHandler.Handle(
            new AderirProdutoCommand("Maria", "98765432100", "m@test.com", 300m),
            CancellationToken.None);

        // Criar cesta: 1 ativo PETR4=100% (diretamente no repo, sem validação de 5 itens)
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var cesta = new CestaRecomendacao("Cesta Teste", new[] { new ItemCesta("PETR4", 100m) });
        await cestaRepo.AdicionarAsync(cesta);

        // Mock CotahistService para retornar cotacao de PETR4 a R$35
        var cotacaoFake = new Cotacao(DateOnly.FromDateTime(DateTime.UtcNow), "PETR4", 34m, 35m, 36m, 33m);
        var cotahistMock = new Mock<ICotahistService>();
        cotahistMock
            .Setup(s => s.ObterCotacoesPorTickers(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { cotacaoFake });

        var kafkaMock = new Mock<IKafkaProducer>();
        kafkaMock
            .Setup(k => k.PublicarAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CriarService(context, cotahistMock.Object, kafkaMock.Object);

        // Executar compra
        // Maria: parcela = TRUNCAR(300/3) = 100, total = 100
        // PETR4 = 100%: valorCesta = 100, qtdBruta = TRUNCAR(100/35) = 2
        // Fracionario = 2 (2 < 100), LotePadrao = 0
        var result = await service.ExecutarCompraAsync(new DateOnly(2026, 3, 5));

        result.Should().NotBeNull();
        result.TotalClientes.Should().Be(1);
        result.TotalConsolidado.Should().Be(100m);
        result.OrdensCompra.Should().HaveCount(1); // 1 ordem fracionária PETR4F
        result.Distribuicoes.Should().HaveCount(1);  // 1 cliente
        result.EventosIRPublicados.Should().Be(1);
        result.Mensagem.Should().Contain("1 clientes");

        // Verificar que cotação foi persistida
        var cotacaoRepo = new CotacaoRepository(context);
        var cotacoes = (await cotacaoRepo.ObterUltimasCotacoesPorTickersAsync(new[] { "PETR4" })).ToList();
        cotacoes.Should().HaveCount(1);
        cotacoes[0].PrecoFechamento.Should().Be(35m);
    }

    [Fact]
    public async Task ExecutarCompraAsync_SemCotacaoDisponivel_ContinuaIgnorandoTicker()
    {
        using var context = TestDbContextFactory.Create();

        // Criar conta master
        var contaRepo = new ContaGraficaRepository(context);
        var master = ContaGrafica.CriarMaster();
        await contaRepo.AdicionarAsync(master);

        // Criar cliente
        var clienteRepo = new ClienteRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        await adesaoHandler.Handle(
            new AderirProdutoCommand("Ana", "11122233344", "ana@test.com", 300m),
            CancellationToken.None);

        // Criar cesta com PETR4
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var cesta = new CestaRecomendacao("Cesta Sem Cotacao", new[] { new ItemCesta("PETR4", 100m) });
        await cestaRepo.AdicionarAsync(cesta);

        // CotahistService retorna vazio (sem cotações) e DB sem cotações
        var cotahistMock = new Mock<ICotahistService>();
        cotahistMock
            .Setup(s => s.ObterCotacoesPorTickers(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Enumerable.Empty<Cotacao>());

        var service = CriarService(context, cotahistMock.Object);

        var result = await service.ExecutarCompraAsync(new DateOnly(2026, 3, 5));

        result.Should().NotBeNull();
        result.TotalClientes.Should().Be(1);
        result.OrdensCompra.Should().BeEmpty(); // Nenhuma ordem sem cotação
    }

    [Fact]
    public async Task ExecutarCompraAsync_ComResiduoMasterExistente_DescontaDaQuantidadeAComprar()
    {
        using var context = TestDbContextFactory.Create();

        var contaRepo = new ContaGraficaRepository(context);
        var master = ContaGrafica.CriarMaster();
        await contaRepo.AdicionarAsync(master);

        var clienteRepo = new ClienteRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        await adesaoHandler.Handle(
            new AderirProdutoCommand("Pedro", "55544433322", "p@test.com", 300m),
            CancellationToken.None);

        // Criar resíduo na conta master (3 ações de PETR4 já em custódia)
        var custodiaRepo = new CustodiaRepository(context);
        var custodiaResiduo = new Custodia(master.Id, "PETR4");
        custodiaResiduo.AdicionarAtivos(3, 35m);
        await custodiaRepo.AdicionarAsync(custodiaResiduo);

        var cestaRepo = new CestaRecomendacaoRepository(context);
        var cesta = new CestaRecomendacao("Cesta Residuo", new[] { new ItemCesta("PETR4", 100m) });
        await cestaRepo.AdicionarAsync(cesta);

        var cotacaoFake = new Cotacao(DateOnly.FromDateTime(DateTime.UtcNow), "PETR4", 34m, 35m, 36m, 33m);
        var cotahistMock = new Mock<ICotahistService>();
        cotahistMock
            .Setup(s => s.ObterCotacoesPorTickers(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { cotacaoFake });

        var service = CriarService(context, cotahistMock.Object);

        // Pedro: parcela = 100, PETR4 = 100%: valorCesta = 100, qtdBruta = 2
        // saldoMaster = 3 (residuo), qtdComprar = max(0, 2 - 3) = 0
        // qtdDisponivel = 0 + 3 = 3
        var result = await service.ExecutarCompraAsync(new DateOnly(2026, 3, 5));

        result.Should().NotBeNull();
        // qtdComprar = max(0, 2 - 3) = 0 → nenhuma ordem de compra executada, nenhum IR gerado
        result.OrdensCompra.Should().HaveCount(1); // DTO do ticker ainda gerado (qtd=0)
        result.OrdensCompra.First().QuantidadeTotal.Should().Be(0);
        result.EventosIRPublicados.Should().Be(0);
    }
}
