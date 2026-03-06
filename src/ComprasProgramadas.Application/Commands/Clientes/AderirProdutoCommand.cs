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
    private readonly IHistoricoAporteRepository _historicoRepo;
    private readonly ICustodiaRepository _custodiaRepo;
    private readonly ICestaRecomendacaoRepository _cestaRepo;

    public AderirProdutoHandler(
        IClienteRepository clienteRepo,
        IContaGraficaRepository contaRepo,
        IHistoricoAporteRepository historicoRepo,
        ICustodiaRepository custodiaRepo,
        ICestaRecomendacaoRepository cestaRepo)
    {
        _clienteRepo = clienteRepo;
        _contaRepo = contaRepo;
        _historicoRepo = historicoRepo;
        _custodiaRepo = custodiaRepo;
        _cestaRepo = cestaRepo;
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

        // RN-004: Criar custódias filhote (Qtd=0) para cada ativo da cesta ativa
        var cesta = await _cestaRepo.ObterAtivaAsync(ct);
        if (cesta != null)
        {
            foreach (var item in cesta.Itens)
                await _custodiaRepo.AdicionarAsync(new Custodia(conta.Id, item.Ticker), ct);
        }

        // RN-013: Registrar o valor inicial na adesão (valorAnterior = 0)
        await _historicoRepo.AdicionarAsync(new HistoricoAporte(cliente.Id, 0m, cliente.ValorMensal), ct);

        return new AdesaoResponse(
            cliente.Id, cliente.Nome, cliente.CPF, cliente.Email,
            cliente.ValorMensal, cliente.Ativo, cliente.DataAdesao,
            new ContaGraficaDto(conta.Id, conta.NumeroConta, conta.Tipo.ToString(), conta.DataCriacao));
    }
}
