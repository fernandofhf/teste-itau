using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using FluentAssertions;

namespace ComprasProgramadas.UnitTests.Domain;

public class CestaRecomendacaoTests
{
    private static IEnumerable<ItemCesta> CriarItens5()
    {
        return new[]
        {
            new ItemCesta("PETR4", 30m),
            new ItemCesta("VALE3", 25m),
            new ItemCesta("ITUB4", 20m),
            new ItemCesta("BBDC4", 15m),
            new ItemCesta("WEGE3", 10m),
        };
    }

    [Fact]
    public void Construtor_CriaComoAtiva()
    {
        var cesta = new CestaRecomendacao("Top Five Março", CriarItens5());

        cesta.Ativa.Should().BeTrue();
        cesta.DataDesativacao.Should().BeNull();
        cesta.Itens.Should().HaveCount(5);
    }

    [Fact]
    public void Desativar_SetaAtivaFalse()
    {
        var cesta = new CestaRecomendacao("Top Five", CriarItens5());
        cesta.Desativar();

        cesta.Ativa.Should().BeFalse();
        cesta.DataDesativacao.Should().NotBeNull();
        cesta.DataDesativacao!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Itens_SomaDosPercentuais_DeveSerCem()
    {
        var cesta = new CestaRecomendacao("Top Five", CriarItens5());

        var soma = cesta.Itens.Sum(i => i.Percentual);
        soma.Should().Be(100m);
    }
}

public class EventoIRTests
{
    [Fact]
    public void Construtor_IniciaComPublicadoFalse()
    {
        var evento = new EventoIR(1, TipoEventoIR.DedoDuro, 3500m, 0.18m);

        evento.PublicadoKafka.Should().BeFalse();
        evento.ValorBase.Should().Be(3500m);
        evento.ValorIR.Should().Be(0.18m);
    }

    [Fact]
    public void MarcarPublicado_SetaPublicadoTrue()
    {
        var evento = new EventoIR(1, TipoEventoIR.DedoDuro, 1000m, 0.05m);
        evento.MarcarPublicado();

        evento.PublicadoKafka.Should().BeTrue();
    }

    [Fact]
    public void Construtor_VendaAcoes_TipoCorreto()
    {
        var evento = new EventoIR(1, TipoEventoIR.VendaAcoes, 25_000m, 1_000m);

        evento.Tipo.Should().Be(TipoEventoIR.VendaAcoes);
    }
}

public class ContaGraficaTests
{
    [Fact]
    public void CriarMaster_CriaConta_ComTipoMaster()
    {
        var conta = ContaGrafica.CriarMaster();

        conta.Tipo.Should().Be(TipoConta.Master);
        conta.NumeroConta.Should().NotBeNullOrEmpty();
        conta.ClienteId.Should().BeNull();
    }

    [Fact]
    public void Construtor_CriaContaFilhote_ComClienteId()
    {
        var conta = new ContaGrafica(42, "FILHOTE-000042", TipoConta.Filhote);

        conta.Tipo.Should().Be(TipoConta.Filhote);
        conta.ClienteId.Should().Be(42);
        conta.NumeroConta.Should().Be("FILHOTE-000042");
    }

    [Fact]
    public void GerarNumeroFilhote_RetornaStringComClienteId()
    {
        var numero = ContaGrafica.GerarNumeroFilhote(99);

        numero.Should().Contain("99");
    }
}

public class OrdemCompraTests
{
    [Fact]
    public void ValorTotal_RetornaQtdVezesPreco()
    {
        var ordem = new OrdemCompra(1, "PETR4", 100, 35.50m, TipoMercado.LotePadrao);

        ordem.ValorTotal.Should().Be(3550m);
    }

    [Fact]
    public void Construtor_SalvaPropriedadesCorretamente()
    {
        var ordem = new OrdemCompra(1, "VALE3F", 50, 72.00m, TipoMercado.Fracionario);

        ordem.ContaMasterId.Should().Be(1);
        ordem.Ticker.Should().Be("VALE3F");
        ordem.Quantidade.Should().Be(50);
        ordem.PrecoUnitario.Should().Be(72.00m);
        ordem.TipoMercado.Should().Be(TipoMercado.Fracionario);
    }
}

public class RebalanceamentoEntityTests
{
    [Fact]
    public void Construtor_SalvaPropriedadesCorretamente()
    {
        var rb = new Rebalanceamento(1, TipoRebalanceamento.MudancaCesta, "PETR4", "VALE3", 5000m);

        rb.ClienteId.Should().Be(1);
        rb.Tipo.Should().Be(TipoRebalanceamento.MudancaCesta);
        rb.TickerVendido.Should().Be("PETR4");
        rb.TickerComprado.Should().Be("VALE3");
        rb.ValorVenda.Should().Be(5000m);
    }
}
