using ComprasProgramadas.Domain.Entities;
using FluentAssertions;

namespace ComprasProgramadas.UnitTests.Domain;

public class ClienteTests
{
    [Theory]
    [InlineData(1000.00, 333.33)]   // 1000/3 = 333.333... → truncado para 333.33
    [InlineData(300.00, 100.00)]    // 300/3 = 100.00 exato
    [InlineData(100.00, 33.33)]     // 100/3 = 33.333... → truncado para 33.33
    [InlineData(1500.00, 500.00)]   // 1500/3 = 500.00 exato
    [InlineData(250.00, 83.33)]     // 250/3 = 83.333... → truncado para 83.33
    public void CalcularValorParcela_TruncaCorretamente(decimal valorMensal, decimal esperado)
    {
        var cliente = new Cliente("Teste", "12345678901", "teste@email.com", valorMensal);

        var parcela = cliente.CalcularValorParcela();

        parcela.Should().Be(esperado);
    }

    [Fact]
    public void Sair_SetaAtivoFalseEDataSaida()
    {
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000.00m);
        cliente.Ativo.Should().BeTrue();

        cliente.Sair();

        cliente.Ativo.Should().BeFalse();
        cliente.DataSaida.Should().NotBeNull();
        cliente.DataSaida!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AlterarValorMensal_AtualizaValorCorretamente()
    {
        var cliente = new Cliente("Maria", "98765432100", "maria@email.com", 500.00m);

        cliente.AlterarValorMensal(1200.00m);

        cliente.ValorMensal.Should().Be(1200.00m);
    }

    [Fact]
    public void Construtor_CriaClienteAtivo()
    {
        var cliente = new Cliente("Carlos", "11122233344", "carlos@test.com", 800.00m);

        cliente.Ativo.Should().BeTrue();
        cliente.DataSaida.Should().BeNull();
        cliente.DataAdesao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
