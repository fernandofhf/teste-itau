using ComprasProgramadas.Application.Commands.Admin;
using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Queries.Admin;
using ComprasProgramadas.Application.Services.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Repositories;
using ComprasProgramadas.IntegrationTests.Helpers;
using FluentAssertions;
using Moq;

namespace ComprasProgramadas.IntegrationTests.Handlers;

public class CriarCestaHandlerTests
{
    private static List<ItemCestaRequest> ItensValidos() => new()
    {
        new("PETR4", 30m), new("VALE3", 25m), new("ITUB4", 20m),
        new("BBDC4", 15m), new("WEGE3", 10m),
    };

    [Fact]
    public async Task Handle_PrimeiraCesta_CriaAtivaSemRebalanceamento()
    {
        using var context = TestDbContextFactory.Create();
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var rebalService = Mock.Of<IRebalanceamentoService>();

        var handler = new CriarCestaHandler(cestaRepo, rebalService);
        var command = new CriarCestaCommand("Top Five Março 2026", ItensValidos());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Ativa.Should().BeTrue();
        result.RebalanceamentoDisparado.Should().BeFalse();
        result.Itens.Should().HaveCount(5);
        result.Mensagem.Should().Contain("Primeira cesta");
    }

    [Fact]
    public async Task Handle_SegundaCesta_DesativaAnteriorEDisparaRebalanceamento()
    {
        using var context = TestDbContextFactory.Create();
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var rebalServiceMock = new Mock<IRebalanceamentoService>();
        rebalServiceMock
            .Setup(s => s.ExecutarRebalanceamentoPorMudancaCestaAsync(
                It.IsAny<ComprasProgramadas.Domain.Entities.CestaRecomendacao>(),
                It.IsAny<ComprasProgramadas.Domain.Entities.CestaRecomendacao>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutarRebalanceamentoResponse(0, "Ok"));

        var handler = new CriarCestaHandler(cestaRepo, rebalServiceMock.Object);

        // Criar primeira cesta
        await handler.Handle(new CriarCestaCommand("Cesta 1", ItensValidos()), CancellationToken.None);

        // Criar segunda cesta
        var novasItens = new List<ItemCestaRequest>
        {
            new("MGLU3", 20m), new("VALE3", 30m), new("ITUB4", 20m),
            new("BBDC4", 15m), new("WEGE3", 15m),
        };
        var result = await handler.Handle(
            new CriarCestaCommand("Cesta 2", novasItens), CancellationToken.None);

        result.RebalanceamentoDisparado.Should().BeTrue();
        result.Mensagem.Should().Contain("Rebalanceamento");

        rebalServiceMock.Verify(s => s.ExecutarRebalanceamentoPorMudancaCestaAsync(
            It.IsAny<ComprasProgramadas.Domain.Entities.CestaRecomendacao>(),
            It.IsAny<ComprasProgramadas.Domain.Entities.CestaRecomendacao>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class GetCestaAtualHandlerTests
{
    private static List<ItemCestaRequest> ItensValidos() => new()
    {
        new("PETR4", 30m), new("VALE3", 25m), new("ITUB4", 20m),
        new("BBDC4", 15m), new("WEGE3", 10m),
    };

    [Fact]
    public async Task Handle_ComCestaAtiva_RetornaCesta()
    {
        using var context = TestDbContextFactory.Create();
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var rebalService = Mock.Of<IRebalanceamentoService>();
        var cotacaoRepo = new CotacaoRepository(context);

        // Criar cesta
        var criarHandler = new CriarCestaHandler(cestaRepo, rebalService);
        await criarHandler.Handle(
            new CriarCestaCommand("Top Five", ItensValidos()),
            CancellationToken.None);

        var queryHandler = new GetCestaAtualHandler(cestaRepo, cotacaoRepo);
        var result = await queryHandler.Handle(new GetCestaAtualQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Nome.Should().Be("Top Five");
        result.Ativa.Should().BeTrue();
        result.Itens.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_SemCestaAtiva_LancaKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var cestaRepo = new CestaRecomendacaoRepository(context);
        var cotacaoRepo = new CotacaoRepository(context);

        var queryHandler = new GetCestaAtualHandler(cestaRepo, cotacaoRepo);

        var act = async () => await queryHandler.Handle(new GetCestaAtualQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
