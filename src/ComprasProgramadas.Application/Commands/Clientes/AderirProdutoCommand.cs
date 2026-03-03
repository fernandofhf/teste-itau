using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ComprasProgramadas.Application.Commands.Clientes;

public record AderirProdutoCommand(string Nome, string CPF, string Email, decimal ValorMensal) : IRequest<AdesaoResponse>;

public class AderirProdutoValidator : AbstractValidator<AderirProdutoCommand>
{
    public AderirProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CPF).NotEmpty().Length(11).Matches(@"^\d{11}$");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.ValorMensal).GreaterThanOrEqualTo(100).WithMessage("O valor mensal mínimo é de R$ 100,00.");
    }
}

public class AderirProdutoHandler : IRequestHandler<AderirProdutoCommand, AdesaoResponse>
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IContaGraficaRepository _contaRepo;

    public AderirProdutoHandler(IClienteRepository clienteRepo, IContaGraficaRepository contaRepo)
    {
        _clienteRepo = clienteRepo;
        _contaRepo = contaRepo;
    }

    public async Task<AdesaoResponse> Handle(AderirProdutoCommand request, CancellationToken ct)
    {
        var existente = await _clienteRepo.ObterPorCpfAsync(request.CPF, ct);
        if (existente != null)
            throw new InvalidOperationException("CPF já cadastrado no sistema.");

        var cliente = new Cliente(request.Nome, request.CPF, request.Email, request.ValorMensal);
        await _clienteRepo.AdicionarAsync(cliente, ct);

        var numeroConta = ContaGrafica.GerarNumeroFilhote(cliente.Id);
        var conta = new ContaGrafica(cliente.Id, numeroConta, TipoConta.Filhote);
        await _contaRepo.AdicionarAsync(conta, ct);

        return new AdesaoResponse(
            cliente.Id, cliente.Nome, cliente.CPF, cliente.Email,
            cliente.ValorMensal, cliente.Ativo, cliente.DataAdesao,
            new ContaGraficaDto(conta.Id, conta.NumeroConta, conta.Tipo.ToString(), conta.DataCriacao));
    }
}
