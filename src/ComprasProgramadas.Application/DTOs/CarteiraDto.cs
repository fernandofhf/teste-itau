namespace ComprasProgramadas.Application.DTOs;

public record CarteiraResponse(
    long ClienteId,
    string Nome,
    string ContaGrafica,
    DateTime DataConsulta,
    ResumoCarteiraDto Resumo,
    IEnumerable<AtivoCarteiraDto> Ativos);

public record ResumoCarteiraDto(
    decimal ValorTotalInvestido,
    decimal ValorAtualCarteira,
    decimal PlTotal,
    decimal RentabilidadePercentual);

public record AtivoCarteiraDto(
    string Ticker,
    int Quantidade,
    decimal PrecoMedio,
    decimal CotacaoAtual,
    decimal ValorAtual,
    decimal Pl,
    decimal PlPercentual,
    decimal ComposicaoCarteira);
