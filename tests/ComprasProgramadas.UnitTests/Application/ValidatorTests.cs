using ComprasProgramadas.Application.Commands.Clientes;
using ComprasProgramadas.Application.Commands.Admin;
using ComprasProgramadas.Application.DTOs;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ComprasProgramadas.UnitTests.Application;

public class AdesaoValidatorTests
{
    private readonly AderirProdutoValidator _validator = new();

    [Fact]
    public void Valido_QuandoTodosCamposCorretos()
    {
        var command = new AderirProdutoCommand("João Silva", "12345678901", "joao@test.com", 500m);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalido_QuandoCPFMenorQue11Digitos()
    {
        var command = new AderirProdutoCommand("João", "1234567890", "joao@test.com", 500m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.CPF);
    }

    [Fact]
    public void Invalido_QuandoCPFMaiorQue11Digitos()
    {
        var command = new AderirProdutoCommand("João", "123456789012", "joao@test.com", 500m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.CPF);
    }

    [Fact]
    public void Invalido_QuandoEmailInvalido()
    {
        var command = new AderirProdutoCommand("João", "12345678901", "nao-é-email", 500m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Invalido_QuandoValorMensalAbaixoDe100()
    {
        var command = new AderirProdutoCommand("João", "12345678901", "joao@test.com", 99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.ValorMensal);
    }

    [Fact]
    public void Valido_QuandoValorMensalExatamente100()
    {
        var command = new AderirProdutoCommand("João", "12345678901", "joao@test.com", 100m);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(c => c.ValorMensal);
    }

    [Fact]
    public void Invalido_QuandoNomeVazio()
    {
        var command = new AderirProdutoCommand("", "12345678901", "joao@test.com", 500m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Nome);
    }
}

public class CriarCestaValidatorTests
{
    private readonly CriarCestaValidator _validator = new();

    [Fact]
    public void Valido_QuandoCincoItensComSoma100()
    {
        var itens = new List<ItemCestaRequest>
        {
            new("PETR4", 30),
            new("VALE3", 25),
            new("ITUB4", 20),
            new("BBDC4", 15),
            new("WEGE3", 10),
        };
        var command = new CriarCestaCommand("Top Five 2026", itens);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalido_QuandoMenosDeCincoItens()
    {
        var itens = new List<ItemCestaRequest>
        {
            new("PETR4", 50),
            new("VALE3", 50),
        };
        var command = new CriarCestaCommand("Cesta", itens);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Itens);
    }

    [Fact]
    public void Invalido_QuandoMaisDeCincoItens()
    {
        var itens = new List<ItemCestaRequest>
        {
            new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
            new("BBDC4", 20), new("WEGE3", 10), new("MGLU3", 10)
        };
        var command = new CriarCestaCommand("Cesta", itens);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Itens);
    }

    [Fact]
    public void Invalido_QuandoSomaDosPercentuaisNaoE100()
    {
        var itens = new List<ItemCestaRequest>
        {
            new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
            new("BBDC4", 20), new("WEGE3", 15)  // soma = 95, não 100
        };
        var command = new CriarCestaCommand("Cesta", itens);
        var result = _validator.TestValidate(command);
        result.Errors.Should().NotBeEmpty();
    }
}

public class AlterarValorMensalValidatorTests
{
    private readonly AlterarValorMensalValidator _validator = new();

    [Fact]
    public void Valido_QuandoNovoValorE100()
    {
        var command = new AlterarValorMensalCommand(1L, 100m);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valido_QuandoNovoValorAcimaDe100()
    {
        var command = new AlterarValorMensalCommand(1L, 500m);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalido_QuandoNovoValorAbaixoDe100()
    {
        var command = new AlterarValorMensalCommand(1L, 99.99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.NovoValorMensal);
    }

    [Fact]
    public void Invalido_QuandoNovoValorZero()
    {
        var command = new AlterarValorMensalCommand(1L, 0m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.NovoValorMensal);
    }
}
