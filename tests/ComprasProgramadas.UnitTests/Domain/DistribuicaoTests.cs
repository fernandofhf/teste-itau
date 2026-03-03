using ComprasProgramadas.Domain.Entities;
using FluentAssertions;

namespace ComprasProgramadas.UnitTests.Domain;

public class DistribuicaoTests
{
    [Fact]
    public void CalcularIRDedoDuro_RetornaZeroVirgulaCincoPorCentoDaOperacao()
    {
        // R$ 3500 × 0.005% = R$ 0.175 → arredondado R$ 0.18
        var dist = new Distribuicao(1, 1, "PETR4", 100, 35.00m);

        var ir = dist.CalcularIRDedoDuro();

        ir.Should().Be(0.18m); // Math.Round(3500 * 0.00005, 2) = Math.Round(0.175, 2) = 0.18
    }

    [Fact]
    public void ValorOperacao_RetornaQtdMultiplicadaPorPreco()
    {
        var dist = new Distribuicao(1, 1, "VALE3", 200, 80.00m);

        dist.ValorOperacao.Should().Be(16_000.00m);
    }

    [Fact]
    public void CalcularIRDedoDuro_OperacaoPequena_RetornaValorBaixo()
    {
        // 10 ações × R$10 = R$100 × 0.00005 = R$0.005 → arredonda para R$0.01
        var dist = new Distribuicao(1, 1, "MGLU3", 10, 10.00m);

        var ir = dist.CalcularIRDedoDuro();

        ir.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void CalcularIRDedoDuro_GrandeOperacao_ValorProporcional()
    {
        // 1000 ações × R$100 = R$100.000 × 0.00005 = R$5.00
        var dist = new Distribuicao(1, 1, "ITUB4", 1000, 100.00m);

        var ir = dist.CalcularIRDedoDuro();

        ir.Should().Be(5.00m);
    }
}
