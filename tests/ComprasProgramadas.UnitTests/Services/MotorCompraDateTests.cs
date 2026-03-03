using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ComprasProgramadas.UnitTests.Services;

public class MotorCompraDateTests
{
    private readonly MotorCompraService _service;

    public MotorCompraDateTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CotacoesPath"] = "cotacoes" })
            .Build();

        _service = new MotorCompraService(
            Mock.Of<IClienteRepository>(),
            Mock.Of<IContaGraficaRepository>(),
            Mock.Of<ICustodiaRepository>(),
            Mock.Of<ICestaRecomendacaoRepository>(),
            Mock.Of<IOrdemCompraRepository>(),
            Mock.Of<IDistribuicaoRepository>(),
            Mock.Of<IEventoIRRepository>(),
            Mock.Of<ICotacaoRepository>(),
            Mock.Of<ICotahistService>(),
            Mock.Of<IKafkaProducer>(),
            config,
            Mock.Of<ILogger<MotorCompraService>>());
    }

    [Theory]
    [InlineData(2026, 3, 5)]    // Quinta-feira
    [InlineData(2026, 3, 16)]   // Segunda (15 cai sábado → avança para 16)
    [InlineData(2026, 3, 25)]   // Quarta-feira
    public void IsDataCompra_DataValida_RetornaTrue(int ano, int mes, int dia)
    {
        var data = new DateOnly(ano, mes, dia);
        _service.IsDataCompra(data).Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 3, 6)]   // Sexta (não é dia alvo)
    [InlineData(2026, 3, 7)]   // Sábado
    [InlineData(2026, 3, 8)]   // Domingo
    [InlineData(2026, 3, 10)]  // Terça (não é dia alvo)
    public void IsDataCompra_DataInvalida_RetornaFalse(int ano, int mes, int dia)
    {
        var data = new DateOnly(ano, mes, dia);
        _service.IsDataCompra(data).Should().BeFalse();
    }

    [Fact]
    public void IsDataCompra_Sabado_RetornaFalse()
    {
        // 15 de março de 2025 cai sábado → a data de compra seria segunda 17/03
        var sabado = new DateOnly(2025, 3, 15);
        sabado.DayOfWeek.Should().Be(DayOfWeek.Saturday);

        _service.IsDataCompra(sabado).Should().BeFalse();
    }

    [Fact]
    public void IsDataCompra_ProximoDiaUtil_QuandoDiaAlvoEFimDeSemana()
    {
        // 15/03/2025 = sábado → avança para segunda 17/03/2025
        var segunda = new DateOnly(2025, 3, 17);
        segunda.DayOfWeek.Should().Be(DayOfWeek.Monday);

        _service.IsDataCompra(segunda).Should().BeTrue();
    }

    [Fact]
    public void ObterProximaDataCompra_QuandoDiaAtualE1_RetornaDia5()
    {
        var dia1 = new DateOnly(2026, 3, 1); // domingo
        var proxima = _service.ObterProximaDataCompra(dia1);

        proxima.Day.Should().BeGreaterThanOrEqualTo(5);
        proxima.Month.Should().Be(3);
        proxima.DayOfWeek.Should().NotBe(DayOfWeek.Saturday);
        proxima.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void ObterProximaDataCompra_QuandoDiaAtualE20_RetornaDia25()
    {
        var dia20 = new DateOnly(2026, 3, 20);
        var proxima = _service.ObterProximaDataCompra(dia20);

        proxima.Day.Should().BeGreaterThanOrEqualTo(25);
        proxima.Month.Should().Be(3);
    }

    [Fact]
    public void ObterProximaDataCompra_QuandoDiaAtualE26_RetornaDia5ProximoMes()
    {
        var dia26 = new DateOnly(2026, 3, 26);
        var proxima = _service.ObterProximaDataCompra(dia26);

        proxima.Month.Should().Be(4);
        proxima.Day.Should().BeGreaterThanOrEqualTo(5);
    }
}
