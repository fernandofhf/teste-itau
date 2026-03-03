using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Commands.Clientes;

public record SairProdutoCommand(long ClienteId) : IRequest<SaidaResponse>;

public class SairProdutoHandler : IRequestHandler<SairProdutoCommand, SaidaResponse>
{
    private readonly IClienteRepository _clienteRepo;

    public SairProdutoHandler(IClienteRepository clienteRepo) => _clienteRepo = clienteRepo;

    public async Task<SaidaResponse> Handle(SairProdutoCommand request, CancellationToken ct)
    {
        var cliente = await _clienteRepo.ObterPorIdAsync(request.ClienteId, ct)
            ?? throw new KeyNotFoundException("Cliente não encontrado.");

        if (!cliente.Ativo)
            throw new InvalidOperationException("Cliente já havia saído do produto.");

        cliente.Sair();
        await _clienteRepo.AtualizarAsync(cliente, ct);

        return new SaidaResponse(
            cliente.Id, cliente.Nome, cliente.Ativo,
            cliente.DataSaida!.Value,
            "Adesão encerrada. Sua posição em custódia foi mantida.");
    }
}
