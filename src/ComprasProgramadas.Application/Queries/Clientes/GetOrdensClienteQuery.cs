using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Queries.Clientes;

public record GetOrdensClienteQuery(long ClienteId) : IRequest<OrdensClienteResponse>;

public class GetOrdensClienteHandler : IRequestHandler<GetOrdensClienteQuery, OrdensClienteResponse>
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IHistoricoOrdemClienteRepository _ordemRepo;

    public GetOrdensClienteHandler(IClienteRepository clienteRepo, IHistoricoOrdemClienteRepository ordemRepo)
    {
        _clienteRepo = clienteRepo;
        _ordemRepo = ordemRepo;
    }

    public async Task<OrdensClienteResponse> Handle(GetOrdensClienteQuery request, CancellationToken ct)
    {
        var cliente = await _clienteRepo.ObterPorIdAsync(request.ClienteId, ct)
            ?? throw new KeyNotFoundException("Cliente não encontrado.");

        var ordens = await _ordemRepo.ObterPorClienteAsync(cliente.Id, ct);

        var items = ordens
            .Select(o => new OrdemClienteDto(
                o.Id,
                o.Ticker,
                o.TipoOrdem.ToString(),
                o.Quantidade,
                o.PrecoUnitario,
                o.ValorTotal,
                o.Origem.ToString(),
                o.DataOperacao))
            .ToList();

        return new OrdensClienteResponse(cliente.Id, items.Count, items);
    }
}
