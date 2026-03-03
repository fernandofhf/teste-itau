using FluentAssertions;

namespace ComprasProgramadas.UnitTests.Services;

/// <summary>
/// Testa as regras de cálculo do IR sobre vendas (rebalanceamento).
/// Conforme RN-050: isenção para vendas mensais ≤ R$20.000.
/// Conforme RN-051: alíquota 20% sobre lucro líquido quando > R$20.000.
/// Conforme RN-052: sem IR se não houver lucro (prejuízo).
/// </summary>
public class IRCalculatorTests
{
    private static decimal CalcularIRVenda(decimal totalVendasMes, decimal lucroLiquido)
    {
        const decimal limiteIsencao = 20_000m;
        if (totalVendasMes > limiteIsencao && lucroLiquido > 0)
            return Math.Round(lucroLiquido * 0.20m, 2);
        return 0m;
    }

    [Fact]
    public void IR_IsentoQuandoVendasAbaixoDe20k()
    {
        var ir = CalcularIRVenda(totalVendasMes: 19_999.99m, lucroLiquido: 5_000m);

        ir.Should().Be(0m);
    }

    [Fact]
    public void IR_IsentoQuandoVendasIgualA20k()
    {
        var ir = CalcularIRVenda(totalVendasMes: 20_000m, lucroLiquido: 3_000m);

        ir.Should().Be(0m);
    }

    [Fact]
    public void IR_Tributa20PorCentoQuandoVendasAcimaDe20k()
    {
        // Vendas = R$25.000, lucro = R$5.000 → IR = R$5.000 × 20% = R$1.000
        var ir = CalcularIRVenda(totalVendasMes: 25_000m, lucroLiquido: 5_000m);

        ir.Should().Be(1_000m);
    }

    [Fact]
    public void IR_ZeroQuandoHaPrejuizo_MesmoAcimaDe20k()
    {
        // Vendas acima de R$20k mas com prejuízo → isento
        var ir = CalcularIRVenda(totalVendasMes: 30_000m, lucroLiquido: -500m);

        ir.Should().Be(0m);
    }

    [Fact]
    public void IR_ZeroQuandoLucroZero_MesmoAcimaDe20k()
    {
        var ir = CalcularIRVenda(totalVendasMes: 50_000m, lucroLiquido: 0m);

        ir.Should().Be(0m);
    }

    [Fact]
    public void IR_AliquotaExata20PorCento()
    {
        // Vendas = R$100.000, lucro = R$10.000 → IR = R$2.000
        var ir = CalcularIRVenda(totalVendasMes: 100_000m, lucroLiquido: 10_000m);

        ir.Should().Be(2_000m);
    }
}
