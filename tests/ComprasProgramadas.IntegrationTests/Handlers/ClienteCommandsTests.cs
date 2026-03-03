using ComprasProgramadas.Application.Commands.Clientes;
using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Infrastructure.Persistence.Repositories;
using ComprasProgramadas.IntegrationTests.Helpers;
using FluentAssertions;
using Moq;

namespace ComprasProgramadas.IntegrationTests.Handlers;

public class AderirProdutoHandlerTests
{
    [Fact]
    public async Task Handle_CPFValido_CriaClienteEContaGrafica()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);

        var handler = new AderirProdutoHandler(clienteRepo, contaRepo);
        var command = new AderirProdutoCommand("João Silva", "12345678901", "joao@test.com", 1000m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ClienteId.Should().BeGreaterThan(0);
        result.Nome.Should().Be("João Silva");
        result.Ativo.Should().BeTrue();
        result.ContaGrafica.Should().NotBeNull();
        result.ContaGrafica.Tipo.Should().Be("Filhote");

        var clienteNoBanco = await clienteRepo.ObterPorIdAsync(result.ClienteId);
        clienteNoBanco.Should().NotBeNull();
        clienteNoBanco!.CPF.Should().Be("12345678901");
    }

    [Fact]
    public async Task Handle_CPFDuplicado_LancaInvalidOperationException()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);

        var handler = new AderirProdutoHandler(clienteRepo, contaRepo);

        await handler.Handle(new AderirProdutoCommand("João", "11122233344", "j@test.com", 500m), CancellationToken.None);

        var act = async () => await handler.Handle(
            new AderirProdutoCommand("João 2", "11122233344", "j2@test.com", 600m),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CPF*");
    }
}

public class SairProdutoHandlerTests
{
    [Fact]
    public async Task Handle_ClienteExistente_SetaAtivoFalse()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);

        // Criar cliente primeiro
        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo);
        var adesaoResult = await adesaoHandler.Handle(
            new AderirProdutoCommand("Maria", "98765432100", "m@test.com", 500m),
            CancellationToken.None);

        var sairHandler = new SairProdutoHandler(clienteRepo);
        var result = await sairHandler.Handle(
            new SairProdutoCommand(adesaoResult.ClienteId),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Ativo.Should().BeFalse();

        var clienteNoBanco = await clienteRepo.ObterPorIdAsync(adesaoResult.ClienteId);
        clienteNoBanco!.Ativo.Should().BeFalse();
        clienteNoBanco.DataSaida.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ClienteInexistente_LancaKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);

        var handler = new SairProdutoHandler(clienteRepo);

        var act = async () => await handler.Handle(
            new SairProdutoCommand(9999),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class AlterarValorMensalHandlerTests
{
    [Fact]
    public async Task Handle_ClienteExistente_AtualizaValorMensal()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);
        var contaRepo = new ContaGraficaRepository(context);

        var adesaoHandler = new AderirProdutoHandler(clienteRepo, contaRepo);
        var adesao = await adesaoHandler.Handle(
            new AderirProdutoCommand("Carlos", "55544433322", "c@test.com", 500m),
            CancellationToken.None);

        var handler = new AlterarValorMensalHandler(clienteRepo);
        var result = await handler.Handle(
            new AlterarValorMensalCommand(adesao.ClienteId, 1200m),
            CancellationToken.None);

        result.ValorMensalNovo.Should().Be(1200m);

        var clienteNoBanco = await clienteRepo.ObterPorIdAsync(adesao.ClienteId);
        clienteNoBanco!.ValorMensal.Should().Be(1200m);
    }

    [Fact]
    public async Task Handle_ClienteInexistente_LancaKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var clienteRepo = new ClienteRepository(context);

        var handler = new AlterarValorMensalHandler(clienteRepo);

        var act = async () => await handler.Handle(
            new AlterarValorMensalCommand(9999, 1000m),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
