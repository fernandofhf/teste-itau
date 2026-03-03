using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Domain.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Queries.Admin;

public record GetHistoricoCestasQuery : IRequest<HistoricoCestasResponse>;

public class GetHistoricoCestasHandler : IRequestHandler<GetHistoricoCestasQuery, HistoricoCestasResponse>
{
    private readonly ICestaRecomendacaoRepository _cestaRepo;

    public GetHistoricoCestasHandler(ICestaRecomendacaoRepository cestaRepo) => _cestaRepo = cestaRepo;

    public async Task<HistoricoCestasResponse> Handle(GetHistoricoCestasQuery request, CancellationToken ct)
    {
        var cestas = await _cestaRepo.ObterHistoricoAsync(ct);
        return new HistoricoCestasResponse(
            cestas.Select(c => new CestaResponse(
                c.Id, c.Nome, c.Ativa, c.DataCriacao, c.DataDesativacao,
                c.Itens.Select(i => new ItemCestaResponse(i.Ticker, i.Percentual)),
                false, null, null, string.Empty)));
    }
}
