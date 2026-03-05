using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ComprasProgramadas.Application.Commands.Clientes;

public record AlterarValorMensalCommand(long ClienteId, decimal NovoValorMensal) : IRequest<AlterarValorMensalResponse>;

public class AlterarValorMensalValidator : AbstractValidator<AlterarValorMensalCommand>
{
    public AlterarValorMensalValidator()
    {
        RuleFor(x => x.NovoValorMensal).GreaterThanOrEqualTo(100).WithMessage("O valor mensal mínimo é de R$ 100,00.");
    }
}

public class AlterarValorMensalHandler : IRequestHandler<AlterarValorMensalCommand, AlterarValorMensalResponse>
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IHistoricoAporteRepository _historicoRepo;

    public AlterarValorMensalHandler(IClienteRepository clienteRepo, IHistoricoAporteRepository historicoRepo)
    {
        _clienteRepo = clienteRepo;
        _historicoRepo = historicoRepo;
    }

    public async Task<AlterarValorMensalResponse> Handle(AlterarValorMensalCommand request, CancellationToken ct)
    {
        var cliente = await _clienteRepo.ObterPorIdAsync(request.ClienteId, ct)
            ?? throw new KeyNotFoundException("Cliente não encontrado.");

        var valorAnterior = cliente.ValorMensal;
        cliente.AlterarValorMensal(request.NovoValorMensal);
        await _clienteRepo.AtualizarAsync(cliente, ct);

        // RN-013: Manter histórico de alterações do valor mensal
        await _historicoRepo.AdicionarAsync(new HistoricoAporte(cliente.Id, valorAnterior, cliente.ValorMensal), ct);

        return new AlterarValorMensalResponse(
            cliente.Id, valorAnterior, cliente.ValorMensal,
            DateTime.UtcNow,
            "Valor mensal atualizado. O novo valor será considerado a partir da próxima data de compra.");
    }
}
