namespace ComprasProgramadas.Application.DTOs;

public record AdesaoRequest(string Nome, string CPF, string Email, decimal ValorMensal);

public record AdesaoResponse(
    long ClienteId,
    string Nome,
    string CPF,
    string Email,
    decimal ValorMensal,
    bool Ativo,
    DateTime DataAdesao,
    ContaGraficaDto ContaGrafica);

public record SaidaResponse(long ClienteId, string Nome, bool Ativo, DateTime DataSaida, string Mensagem);

public record AlterarValorMensalRequest(decimal NovoValorMensal);

public record AlterarValorMensalResponse(
    long ClienteId,
    decimal ValorMensalAnterior,
    decimal ValorMensalNovo,
    DateTime DataAlteracao,
    string Mensagem);

public record ContaGraficaDto(long Id, string NumeroConta, string Tipo, DateTime DataCriacao);

public record HistoricoAporteItemDto(
    long Id,
    decimal ValorAnterior,
    decimal ValorNovo,
    DateTime DataAlteracao);

public record HistoricoAportesResponse(
    long ClienteId,
    IReadOnlyList<HistoricoAporteItemDto> Historico);

public record OrdemClienteDto(
    long Id,
    string Ticker,
    string TipoOrdem,
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorTotal,
    string Origem,
    DateTime DataOperacao);

public record OrdensClienteResponse(
    long ClienteId,
    int Total,
    IReadOnlyList<OrdemClienteDto> Ordens);
