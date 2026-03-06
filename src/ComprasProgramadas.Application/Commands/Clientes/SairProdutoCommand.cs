using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ComprasProgramadas.Application.Commands.Clientes;

public record SairProdutoCommand(long ClienteId) : IRequest<SaidaResponse>;

public class SairProdutoHandler : IRequestHandler<SairProdutoCommand, SaidaResponse>
{
    private readonly IClienteRepository _clienteRepo;
    private readonly ILogger<SairProdutoHandler>? _logger;

    public SairProdutoHandler(IClienteRepository clienteRepo, ILogger<SairProdutoHandler>? logger = null)
    {
        _clienteRepo = clienteRepo;
        _logger = logger;
    }

    public async Task<SaidaResponse> Handle(SairProdutoCommand request, CancellationToken ct)
    {
        var cliente = await _clienteRepo.ObterPorIdAsync(request.ClienteId, ct)
            ?? throw new KeyNotFoundException("Cliente não encontrado.");

        if (!cliente.Ativo)
            throw new InvalidOperationException("Cliente já havia saído do produto.");

        cliente.Sair();
        await _clienteRepo.AtualizarAsync(cliente, ct);

        _logger?.LogInformation(
            "Cliente encerrou adesão: ClienteId={ClienteId} Nome={Nome} DataSaida={DataSaida}",
            cliente.Id, cliente.Nome, cliente.DataSaida);

        return new SaidaResponse(
            cliente.Id, cliente.Nome, cliente.Ativo,
            cliente.DataSaida!.Value,
            "Adesão encerrada. Sua posição em custódia foi mantida.");
    }
}
