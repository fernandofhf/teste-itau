using FluentAssertions;

namespace ComprasProgramadas.UnitTests.Services;

/// <summary>
/// Testa as regras de cálculo do motor de compra isoladamente (sem dependências externas).
/// </summary>
public class MotorCompraCalculoTests
{
    // Simula a lógica de truncamento do motor (RN-010)
    private static int CalcularQuantidade(decimal valorAtivo, decimal preco)
        => (int)Math.Truncate(valorAtivo / preco);

    // Simula a lógica de separação lote/fracionário (RN-011)
    private static (int LotePadrao, int Fracionario) SepararLotes(int quantidade)
        => ((quantidade / 100) * 100, quantidade % 100);

    [Theory]
    [InlineData(1000.00, 35.00, 28)]  // 1000/35 = 28.57 → truncado = 28
    [InlineData(500.00, 36.50, 13)]   // 500/36.5 = 13.69 → truncado = 13
    [InlineData(300.00, 100.00, 3)]   // 300/100 = 3.00 → 3
    [InlineData(99.00, 100.00, 0)]    // 99/100 = 0.99 → truncado = 0
    public void CalcularQuantidade_TruncaSemArredondar(decimal valor, decimal preco, int esperado)
    {
        CalcularQuantidade(valor, preco).Should().Be(esperado);
    }

    [Theory]
    [InlineData(250, 200, 50)]    // 250 ações: lote=200, fracionário=50
    [InlineData(100, 100, 0)]     // 100 ações: lote=100, fracionário=0
    [InlineData(99, 0, 99)]       // 99 ações: tudo fracionário
    [InlineData(301, 300, 1)]     // 301 ações: 3 lotes + 1 fracionário
    [InlineData(0, 0, 0)]         // 0 ações: nada
    public void SepararLotes_CalculaCorretamenteLoteEFracionario(
        int qtd, int loteEsperado, int fracEsperado)
    {
        var (lote, frac) = SepararLotes(qtd);

        lote.Should().Be(loteEsperado);
        frac.Should().Be(fracEsperado);
    }

    [Fact]
    public void DistribuirProporcionalmente_TruncaSemArredondar()
    {
        // 3 clientes com mesmos aportes: proporção = 1/3 = 0.333...
        // Total disponível = 10 ações
        // Cada cliente recebe: TRUNCAR(0.333... × 10) = TRUNCAR(3.33) = 3
        // Resíduo = 10 - (3×3) = 1 ação na conta master
        decimal totalAportes = 300m;
        var aportes = new[] { 100m, 100m, 100m };
        int totalDisponivel = 10;

        var distribuidos = aportes
            .Select(a => (int)Math.Truncate((a / totalAportes) * totalDisponivel))
            .ToList();

        distribuidos.Should().AllBeEquivalentTo(3);
        var residuo = totalDisponivel - distribuidos.Sum();
        residuo.Should().Be(1);
    }

    [Fact]
    public void DistribuirProporcionalmente_ClientesMaiorAporte_RecebeMais()
    {
        // Cliente A: R$600 (60%), Cliente B: R$400 (40%)
        // Total disponível: 100 ações
        decimal totalAportes = 1000m;
        int totalDisponivel = 100;

        var clienteAQtd = (int)Math.Truncate((600m / totalAportes) * totalDisponivel);
        var clienteBQtd = (int)Math.Truncate((400m / totalAportes) * totalDisponivel);

        clienteAQtd.Should().Be(60);
        clienteBQtd.Should().Be(40);
    }

    [Fact]
    public void ResiduoMaster_PermaneceCustodia_ParaProximaCompra()
    {
        // Simula: 10 cotas disponíveis para 3 clientes iguais
        // Cada um recebe 3 (3+3+3=9), sobra 1 no master
        int totalDisponivel = 10;
        int clientesAtivos = 3;
        decimal totalAportes = 300m;
        var aporteIndividual = 100m;

        int totalDistribuido = 0;
        for (int i = 0; i < clientesAtivos; i++)
        {
            totalDistribuido += (int)Math.Truncate((aporteIndividual / totalAportes) * totalDisponivel);
        }

        int residuo = totalDisponivel - totalDistribuido;
        residuo.Should().Be(1);
    }

    [Fact]
    public void TotalConsolidado_SomaTruncadosDeTodosClientes()
    {
        // Regra: cada cliente contribui com o TRUNCAR(ValorMensal/3)
        var valoresMensais = new[] { 1000m, 500m, 300m };

        var aportes = valoresMensais
            .Select(v => Math.Truncate(v / 3 * 100) / 100)
            .ToList();

        aportes[0].Should().Be(333.33m);  // 1000/3 truncado
        aportes[1].Should().Be(166.66m);  // 500/3 truncado
        aportes[2].Should().Be(100.00m);  // 300/3 exato

        var total = aportes.Sum();
        total.Should().Be(599.99m);
    }
}
