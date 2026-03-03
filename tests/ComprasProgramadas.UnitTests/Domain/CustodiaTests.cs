using ComprasProgramadas.Domain.Entities;
using FluentAssertions;

namespace ComprasProgramadas.UnitTests.Domain;

public class CustodiaTests
{
    [Fact]
    public void AdicionarAtivos_PrimeiraCompra_UsaPrecoCompraComoMedio()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(100, 35.00m);

        custodia.Quantidade.Should().Be(100);
        custodia.PrecoMedio.Should().Be(35.00m);
    }

    [Fact]
    public void AdicionarAtivos_SegundaCompra_RecalculaPrecoMedioPonderado()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(100, 35.00m);  // custo = 3500
        custodia.AdicionarAtivos(100, 37.00m);  // custo = 3700; total = 7200; PM = 36.00

        custodia.Quantidade.Should().Be(200);
        custodia.PrecoMedio.Should().Be(36.00m);
    }

    [Fact]
    public void AdicionarAtivos_QuantidadeZero_NaoAlteraEstado()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(100, 35.00m);
        custodia.AdicionarAtivos(0, 40.00m);

        custodia.Quantidade.Should().Be(100);
        custodia.PrecoMedio.Should().Be(35.00m);
    }

    [Fact]
    public void RemoverAtivos_QuantidadeSuficiente_ReduzQuantidade()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(100, 35.00m);
        custodia.RemoverAtivos(40);

        custodia.Quantidade.Should().Be(60);
        custodia.PrecoMedio.Should().Be(35.00m); // PM não muda em venda
    }

    [Fact]
    public void RemoverAtivos_QuantidadeInsuficiente_LancaExcecao()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(10, 35.00m);

        var act = () => custodia.RemoverAtivos(50);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PETR4*");
    }

    [Fact]
    public void CalcularPL_CotacaoAcimaDaMedia_RetornaPositivo()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(100, 30.00m);

        var pl = custodia.CalcularPL(35.00m);

        pl.Should().Be(500.00m); // (35 - 30) * 100
    }

    [Fact]
    public void CalcularPL_CotacaoAbaixoDaMedia_RetornaNegativo()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(100, 35.00m);

        var pl = custodia.CalcularPL(30.00m);

        pl.Should().Be(-500.00m); // (30 - 35) * 100
    }

    [Fact]
    public void CalcularValorAtual_RetornaQtdMultiplicadaPelaCotacao()
    {
        var custodia = new Custodia(1, "PETR4");
        custodia.AdicionarAtivos(200, 30.00m);

        var valor = custodia.CalcularValorAtual(36.00m);

        valor.Should().Be(7200.00m);
    }

    [Fact]
    public void AdicionarAtivos_TresCompras_PrecoMedioCorreto()
    {
        // 100 @ 30 + 100 @ 34 + 100 @ 38 → PM = (3000+3400+3800)/300 = 34.00
        var custodia = new Custodia(1, "VALE3");
        custodia.AdicionarAtivos(100, 30.00m);
        custodia.AdicionarAtivos(100, 34.00m);
        custodia.AdicionarAtivos(100, 38.00m);

        custodia.Quantidade.Should().Be(300);
        custodia.PrecoMedio.Should().BeApproximately(34.00m, 0.01m);
    }
}
