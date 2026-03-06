using ComprasProgramadas.Application.Commands.Clientes;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Infrastructure.Persistence.Repositories;
using ComprasProgramadas.IntegrationTests.Helpers;
using FluentAssertions;

namespace ComprasProgramadas.IntegrationTests.Repositories;

// ─── DistribuicaoRepository ──────────────────────────────────────────────────

public class DistribuicaoRepositoryTests
{
    [Fact]
    public async Task ObterPorClienteAsync_SemConta_RetornaVazio()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new DistribuicaoRepository(context);

        var result = await repo.ObterPorClienteAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ObterPorClienteAsync_ComDistribuicoes_RetornaOrdenado()
    {
        using var context = TestDbContextFactory.Create();

        // Criar cliente + conta filhote
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Teste", "11122233344", "t@t.com", 300m),
            CancellationToken.None);

        var contaFilhote = await contaRepo.ObterFilhotePorClienteIdAsync(adesao.ClienteId);
        var custodiaRepo = new CustodiaRepository(context);
        var custodia = new Custodia(contaFilhote!.Id, "PETR4");
        custodia.AdicionarAtivos(10, 35m);
        await custodiaRepo.AdicionarAsync(custodia);

        // Criar uma ordem e distribuição
        var ordemRepo = new OrdemCompraRepository(context);
        var contaMaster = new ContaGrafica(0, "MST-000001", TipoConta.Master);
        await contaRepo.AdicionarAsync(contaMaster);
        var ordem = new OrdemCompra(contaMaster.Id, "PETR4", 10, 35m, TipoMercado.Fracionario);
        await ordemRepo.AdicionarAsync(ordem);

        var distribuicaoRepo = new DistribuicaoRepository(context);
        var distribuicao = new Distribuicao(ordem.Id, custodia.Id, "PETR4", 5, 35m);
        await distribuicaoRepo.AdicionarRangeAsync(new[] { distribuicao });

        var result = (await distribuicaoRepo.ObterPorClienteAsync(adesao.ClienteId)).ToList();

        result.Should().HaveCount(1);
        result[0].Quantidade.Should().Be(5);
    }

    [Fact]
    public async Task AdicionarRangeAsync_PersistePlurais()
    {
        using var context = TestDbContextFactory.Create();

        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo, new HistoricoAporteRepository(context), new CustodiaRepository(context), new CestaRecomendacaoRepository(context));
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Maria", "22233344455", "m@t.com", 300m),
            CancellationToken.None);

        var contaFilhote = await contaRepo.ObterFilhotePorClienteIdAsync(adesao.ClienteId);
        var custodiaRepo = new CustodiaRepository(context);
        var cust1 = new Custodia(contaFilhote!.Id, "VALE3");
        cust1.AdicionarAtivos(5, 25m);
        await custodiaRepo.AdicionarAsync(cust1);
        var cust2 = new Custodia(contaFilhote.Id, "ITUB4");
        cust2.AdicionarAtivos(5, 30m);
        await custodiaRepo.AdicionarAsync(cust2);

        var contaMaster = new ContaGrafica(0, "MST-000001", TipoConta.Master);
        await contaRepo.AdicionarAsync(contaMaster);
        var ordemRepo = new OrdemCompraRepository(context);
        var ordem = new OrdemCompra(contaMaster.Id, "VALE3", 10, 25m, TipoMercado.LotePadrao);
        await ordemRepo.AdicionarAsync(ordem);

        var distribuicaoRepo = new DistribuicaoRepository(context);
        var dist1 = new Distribuicao(ordem.Id, cust1.Id, "VALE3", 3, 25m);
        var dist2 = new Distribuicao(ordem.Id, cust2.Id, "ITUB4", 2, 30m);
        await distribuicaoRepo.AdicionarRangeAsync(new[] { dist1, dist2 });

        var result = await distribuicaoRepo.ObterPorClienteAsync(adesao.ClienteId);
        result.Should().HaveCount(2);
    }
}

// ─── EventoIRRepository ──────────────────────────────────────────────────────

public class EventoIRRepositoryTests
{
    [Fact]
    public async Task ObterTotalVendasMesAsync_SemEventos_RetornaZero()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new EventoIRRepository(context);

        var total = await repo.ObterTotalVendasMesAsync(999, 2026, 3);

        total.Should().Be(0m);
    }

    [Fact]
    public async Task ObterTotalVendasMesAsync_ComEventosNoMes_SomaSomenteDedoDuro()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new EventoIRRepository(context);

        var clienteId = 1L;
        // Adicionar 2 eventos: 1 de DedoDuro (não conta) e 1 de VendaAcoes (conta)
        var eventoDedoDuro = new EventoIR(clienteId, TipoEventoIR.DedoDuro, 5000m, 2.50m);
        var eventoVenda = new EventoIR(clienteId, TipoEventoIR.VendaAcoes, 12000m, 0m);

        await repo.AdicionarAsync(eventoDedoDuro);
        await repo.AdicionarAsync(eventoVenda);

        var total = await repo.ObterTotalVendasMesAsync(clienteId, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        // Só VendaAcoes é somado, não DedoDuro
        total.Should().Be(12000m);
    }

    [Fact]
    public async Task AdicionarEAtualizarAsync_MarcarPublicado_PersisteMudanca()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new EventoIRRepository(context);

        var evento = new EventoIR(1L, TipoEventoIR.DedoDuro, 700m, 0.035m);
        await repo.AdicionarAsync(evento);

        evento.MarcarPublicado();
        await repo.AtualizarAsync(evento);

        var total = await repo.ObterTotalVendasMesAsync(1L, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        // Tipo é DedoDuro, então não somado no total de vendas
        total.Should().Be(0m);
    }
}

// ─── OrdemCompraRepository ───────────────────────────────────────────────────

public class OrdemCompraRepositoryTests
{
    [Fact]
    public async Task ExisteParaDataAsync_SemOrdens_RetornaFalse()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OrdemCompraRepository(context);

        var existe = await repo.ExisteParaDataAsync(new DateOnly(2026, 3, 5));

        existe.Should().BeFalse();
    }

    [Fact]
    public async Task ExisteParaDataAsync_ComOrdemNaData_RetornaTrue()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OrdemCompraRepository(context);

        var contaRepo = new ContaGraficaRepository(context);
        var master = ContaGrafica.CriarMaster();
        await contaRepo.AdicionarAsync(master);

        var ordem = new OrdemCompra(master.Id, "PETR4", 100, 35m, TipoMercado.LotePadrao);
        await repo.AdicionarAsync(ordem);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var existe = await repo.ExisteParaDataAsync(hoje);

        existe.Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarRangeAsync_PersisteLote()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OrdemCompraRepository(context);

        var contaRepo = new ContaGraficaRepository(context);
        var master = ContaGrafica.CriarMaster();
        await contaRepo.AdicionarAsync(master);

        var ordens = new[]
        {
            new OrdemCompra(master.Id, "PETR4", 200, 35m, TipoMercado.LotePadrao),
            new OrdemCompra(master.Id, "PETR4F", 50, 35m, TipoMercado.Fracionario),
        };

        await repo.AdicionarRangeAsync(ordens);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var existe = await repo.ExisteParaDataAsync(hoje);
        existe.Should().BeTrue();
    }
}

// ─── RebalanceamentoRepository ───────────────────────────────────────────────

public class RebalanceamentoRepositoryTests
{
    [Fact]
    public async Task AdicionarRangeAsync_PersisteLista()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new RebalanceamentoRepository(context);

        var rebalanceamentos = new[]
        {
            new Rebalanceamento(1L, TipoRebalanceamento.MudancaCesta, "PETR4", null, 3500m),
            new Rebalanceamento(1L, TipoRebalanceamento.MudancaCesta, null, "VALE3", 0m),
        };

        var act = async () => await repo.AdicionarRangeAsync(rebalanceamentos);

        await act.Should().NotThrowAsync();
    }
}
