using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Queries.Clientes;

public record GetHistoricoAportesQuery(long ClienteId) : IRequest<HistoricoAportesResponse>;

public class GetHistoricoAportesHandler : IRequestHandler<GetHistoricoAportesQuery, HistoricoAportesResponse>
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IHistoricoAporteRepository _historicoRepo;

    public GetHistoricoAportesHandler(IClienteRepository clienteRepo, IHistoricoAporteRepository historicoRepo)
    {
        _clienteRepo = clienteRepo;
        _historicoRepo = historicoRepo;
    }

    public async Task<HistoricoAportesResponse> Handle(GetHistoricoAportesQuery request, CancellationToken ct)
    {
        var cliente = await _clienteRepo.ObterPorIdAsync(request.ClienteId, ct)
            ?? throw new KeyNotFoundException("Cliente não encontrado.");

        var historico = await _historicoRepo.ObterPorClienteAsync(cliente.Id, ct);

        var items = historico
            .OrderBy(h => h.DataAlteracao)
            .Select(h => new HistoricoAporteItemDto(h.Id, h.ValorAnterior, h.ValorNovo, h.DataAlteracao))
            .ToList();

        return new HistoricoAportesResponse(cliente.Id, items);
    }
}
