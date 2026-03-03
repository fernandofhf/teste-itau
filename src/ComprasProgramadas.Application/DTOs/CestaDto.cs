namespace ComprasProgramadas.Application.DTOs;

public record CriarCestaRequest(string Nome, IEnumerable<ItemCestaRequest> Itens);

public record ItemCestaRequest(string Ticker, decimal Percentual);

public record CestaResponse(
    long CestaId,
    string Nome,
    bool Ativa,
    DateTime DataCriacao,
    DateTime? DataDesativacao,
    IEnumerable<ItemCestaResponse> Itens,
    bool RebalanceamentoDisparado,
    IEnumerable<string>? AtivosRemovidos,
    IEnumerable<string>? AtivosAdicionados,
    string Mensagem);

public record ItemCestaResponse(string Ticker, decimal Percentual, decimal? CotacaoAtual = null);

public record HistoricoCestasResponse(IEnumerable<CestaResponse> Cestas);
