using ComprasProgramadas.Application.Commands.Admin;
using ComprasProgramadas.Application.Commands.Clientes;
using ComprasProgramadas.Application.Commands.Motor;
using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Queries.Admin;
using ComprasProgramadas.Application.Queries.Clientes;
using ComprasProgramadas.Application.Services.Interfaces;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Infrastructure.Persistence.Repositories;
using ComprasProgramadas.IntegrationTests.Helpers;
using FluentAssertions;
using Moq;

namespace ComprasProgramadas.IntegrationTests.Handlers;

public class GetHistoricoCestasHandlerTests
{
    private static List<ItemCestaRequest> ItensValidos() => new()
    {
        new("PETR4", 30m), new("VALE3", 25m), new("ITUB4", 20m),
        new("BBDC4", 15m), new("WEGE3", 10m),
    };

    [Fact]
    public async Task Handle_SemCestas_RetornaListaVazia()
    {
        using var context = TestDbContextFactory.Create();
        var cestaRepo = new CestaRecomendacaoRepository(context);

        var handler = new GetHistoricoCestasHandler(cestaRepo);
        var result = await handler.Handle(new GetHistoricoCestasQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Cestas.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ComDuasCestas_RetornaHistoricoCompleto()
    {
        using var context = TestDbContextFactory.Create();
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var rebalService = Mock.Of<IRebalanceamentoService>();

        var criarHandler = new CriarCestaHandler(cestaRepo, rebalService);

        await criarHandler.Handle(new CriarCestaCommand("Cesta Janeiro", ItensValidos()), CancellationToken.None);

        var itens2 = new List<ItemCestaRequest>
        {
            new("MGLU3", 20m), new("VALE3", 30m), new("ITUB4", 20m),
            new("BBDC4", 15m), new("WEGE3", 15m),
        };
        var rebalMock = new Mock<IRebalanceamentoService>();
        rebalMock.Setup(s => s.ExecutarRebalanceamentoPorMudancaCestaAsync(It.IsAny<CestaRecomendacao>(), It.IsAny<CestaRecomendacao>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutarRebalanceamentoResponse(0, "Ok"));
        var criarHandler2 = new CriarCestaHandler(cestaRepo, rebalMock.Object);
        await criarHandler2.Handle(new CriarCestaCommand("Cesta Fevereiro", itens2), CancellationToken.None);

        var queryHandler = new GetHistoricoCestasHandler(cestaRepo);
        var result = await queryHandler.Handle(new GetHistoricoCestasQuery(), CancellationToken.None);

        result.Cestas.Should().HaveCount(2);
    }
}

public class GetCarteiraHandlerTests
{
    [Fact]
    public async Task Handle_ClienteSemCustodia_RetornaCarteiraVazia()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var custodiaRepo = new CustodiaRepository(context);
        var cotacaoRepo = new CotacaoRepository(context);

        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Pedro", "33322211100", "p@test.com", 600m),
            CancellationToken.None);

        var handler = new GetCarteiraHandler(clienteRepo, contaRepo, custodiaRepo, cotacaoRepo);
        var result = await handler.Handle(new GetCarteiraQuery(adesao.ClienteId), CancellationToken.None);

        result.Should().NotBeNull();
        result.ClienteId.Should().Be(adesao.ClienteId);
        result.Ativos.Should().BeEmpty();
        result.Resumo.ValorTotalInvestido.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ClienteInexistente_LancaKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var custodiaRepo = new CustodiaRepository(context);
        var cotacaoRepo = new CotacaoRepository(context);

        var handler = new GetCarteiraHandler(clienteRepo, contaRepo, custodiaRepo, cotacaoRepo);

        var act = async () => await handler.Handle(new GetCarteiraQuery(9999), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ClienteComCustodia_RetornaAtivos()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var custodiaRepo = new CustodiaRepository(context);
        var cotacaoRepo = new CotacaoRepository(context);

        // Criar cliente e conta
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Ana Costa", "77788899911", "ana@test.com", 900m),
            CancellationToken.None);

        // Obter conta filhote e criar custódia manualmente
        var conta = await contaRepo.ObterFilhotePorClienteIdAsync(adesao.ClienteId);
        var custodia = new Custodia(conta!.Id, "PETR4");
        custodia.AdicionarAtivos(100, 35.00m);
        await custodiaRepo.AdicionarAsync(custodia);

        var handler = new GetCarteiraHandler(clienteRepo, contaRepo, custodiaRepo, cotacaoRepo);
        var result = await handler.Handle(new GetCarteiraQuery(adesao.ClienteId), CancellationToken.None);

        result.Ativos.Should().HaveCount(1);
        result.Ativos.First().Ticker.Should().Be("PETR4");
        result.Resumo.ValorTotalInvestido.Should().Be(3500m);
    }
}

public class GetCustodiaMasterHandlerTests
{
    [Fact]
    public async Task Handle_SemContaMaster_LancaInvalidOperationException()
    {
        using var context = TestDbContextFactory.Create();
        var contaRepo = new ContaGraficaRepository(context);
        var custodiaRepo = new CustodiaRepository(context);
        var cotacaoRepo = new CotacaoRepository(context);

        var handler = new GetCustodiaMasterHandler(contaRepo, custodiaRepo, cotacaoRepo);

        var act = async () => await handler.Handle(new GetCustodiaMasterQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ComContaMasterSemCustodia_RetornaListaVazia()
    {
        using var context = TestDbContextFactory.Create();
        var contaRepo = new ContaGraficaRepository(context);
        var custodiaRepo = new CustodiaRepository(context);
        var cotacaoRepo = new CotacaoRepository(context);

        // Criar conta master
        var master = ContaGrafica.CriarMaster();
        await contaRepo.AdicionarAsync(master);

        var handler = new GetCustodiaMasterHandler(contaRepo, custodiaRepo, cotacaoRepo);
        var result = await handler.Handle(new GetCustodiaMasterQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Custodia.Should().BeEmpty();
        result.ValorTotalResiduo.Should().Be(0);
    }
}

// ─── GetRentabilidadeHandler ─────────────────────────────────────────────────

public class GetRentabilidadeHandlerTests
{
    [Fact]
    public async Task Handle_ClienteInexistente_LancaKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new GetRentabilidadeHandler(
            new ClienteRepository(context),
            new ContaGraficaRepository(context),
            new CustodiaRepository(context),
            new CotacaoRepository(context),
            new DistribuicaoRepository(context));

        var act = async () => await handler.Handle(new GetRentabilidadeQuery(9999), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Cliente*");
    }

    [Fact]
    public async Task Handle_ClienteSemCustodia_RetornaRentabilidadeZero()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);

        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("João Teste", "11122233344", "j@t.com", 300m),
            CancellationToken.None);

        var handler = new GetRentabilidadeHandler(
            clienteRepo,
            contaRepo,
            new CustodiaRepository(context),
            new CotacaoRepository(context),
            new DistribuicaoRepository(context));

        var result = await handler.Handle(new GetRentabilidadeQuery(adesao.ClienteId), CancellationToken.None);

        result.Should().NotBeNull();
        result.ClienteId.Should().Be(adesao.ClienteId);
        result.Nome.Should().Be("João Teste");
        result.Rentabilidade.ValorTotalInvestido.Should().Be(0m);
        result.Rentabilidade.RentabilidadePercentual.Should().Be(0m);
        result.HistoricoAportes.Should().BeEmpty();
        result.EvolucaoCarteira.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ClienteComCustodiaECotacao_CalculaRentabilidade()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var custodiaRepo = new CustodiaRepository(context);
        var cotacaoRepo = new CotacaoRepository(context);
        var distribuicaoRepo = new DistribuicaoRepository(context);

        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Ana Rend", "44455566677", "a@t.com", 900m),
            CancellationToken.None);

        var contaFilhote = await contaRepo.ObterFilhotePorClienteIdAsync(adesao.ClienteId);

        // Custódia: 100 PETR4 a preço médio R$30
        var custodia = new Custodia(contaFilhote!.Id, "PETR4");
        custodia.AdicionarAtivos(100, 30m);
        await custodiaRepo.AdicionarAsync(custodia);

        // Cotação atual: R$35 (lucro de R$5 por ação)
        var cotacao = new Cotacao(DateOnly.FromDateTime(DateTime.UtcNow), "PETR4", 34m, 35m, 36m, 33m);
        await cotacaoRepo.AdicionarOuAtualizarAsync(new[] { cotacao });

        // Criar uma conta master para a ordem, e adicionar distribuição
        var contaMaster = ContaGrafica.CriarMaster();
        await contaRepo.AdicionarAsync(contaMaster);
        var ordemRepo = new OrdemCompraRepository(context);
        var ordem = new OrdemCompra(contaMaster.Id, "PETR4F", 100, 30m, TipoMercado.Fracionario);
        await ordemRepo.AdicionarAsync(ordem);
        var dist = new Distribuicao(ordem.Id, custodia.Id, "PETR4", 100, 30m);
        await distribuicaoRepo.AdicionarRangeAsync(new[] { dist });

        var handler = new GetRentabilidadeHandler(
            clienteRepo, contaRepo, custodiaRepo, cotacaoRepo, distribuicaoRepo);

        var result = await handler.Handle(new GetRentabilidadeQuery(adesao.ClienteId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Rentabilidade.ValorTotalInvestido.Should().Be(3000m);   // 100 * 30
        result.Rentabilidade.ValorAtualCarteira.Should().Be(3500m);    // 100 * 35
        result.Rentabilidade.PlTotal.Should().Be(500m);                // 3500 - 3000
        result.Rentabilidade.RentabilidadePercentual.Should().BeApproximately(16.67m, 0.01m);
        result.HistoricoAportes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ClienteSemContaGrafica_LancaInvalidOperationException()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);

        // Adicionar cliente diretamente sem conta gráfica
        var cliente = new Cliente("Sem Conta", "77788899900", "sc@t.com", 300m);
        await clienteRepo.AdicionarAsync(cliente);

        var handler = new GetRentabilidadeHandler(
            clienteRepo,
            new ContaGraficaRepository(context),
            new CustodiaRepository(context),
            new CotacaoRepository(context),
            new DistribuicaoRepository(context));

        var act = async () => await handler.Handle(new GetRentabilidadeQuery(cliente.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Conta*");
    }
}

// ─── ExecutarCompraHandler ───────────────────────────────────────────────────

public class ExecutarCompraHandlerTests
{
    [Fact]
    public async Task Handle_UsaDataAtual_QuandoDataNaoFornecida()
    {
        var motorMock = new Mock<IMotorCompraService>();
        motorMock.Setup(m => m.ExecutarCompraAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutarCompraResponse(
                DateTime.UtcNow, 0, 0,
                Enumerable.Empty<OrdemCompraDto>(),
                Enumerable.Empty<DistribuicaoClienteDto>(),
                Enumerable.Empty<ResiduoDto>(), 0, "Nenhum cliente."));

        var handler = new ExecutarCompraHandler(motorMock.Object);
        var result = await handler.Handle(new ExecutarCompraCommand(null), CancellationToken.None);

        result.Should().NotBeNull();
        motorMock.Verify(m => m.ExecutarCompraAsync(
            It.Is<DateOnly>(d => d == DateOnly.FromDateTime(DateTime.UtcNow)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UsaDataFornecida_QuandoInformada()
    {
        var dataEspecifica = new DateOnly(2026, 3, 5);
        var motorMock = new Mock<IMotorCompraService>();
        motorMock.Setup(m => m.ExecutarCompraAsync(dataEspecifica, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutarCompraResponse(
                DateTime.UtcNow, 3, 900m,
                Enumerable.Empty<OrdemCompraDto>(),
                Enumerable.Empty<DistribuicaoClienteDto>(),
                Enumerable.Empty<ResiduoDto>(), 0, "Ok."));

        var handler = new ExecutarCompraHandler(motorMock.Object);
        var result = await handler.Handle(new ExecutarCompraCommand(dataEspecifica), CancellationToken.None);

        result.TotalClientes.Should().Be(3);
        result.TotalConsolidado.Should().Be(900m);
        motorMock.Verify(m => m.ExecutarCompraAsync(dataEspecifica, It.IsAny<CancellationToken>()), Times.Once);
    }
}
