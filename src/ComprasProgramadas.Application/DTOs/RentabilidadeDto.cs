namespace ComprasProgramadas.Application.DTOs;

public record RentabilidadeResponse(
    long ClienteId,
    string Nome,
    DateTime DataConsulta,
    RentabilidadeSummaryDto Rentabilidade,
    IEnumerable<AporteHistoricoDto> HistoricoAportes,
    IEnumerable<EvolucaoCarteiraDto> EvolucaoCarteira);

public record RentabilidadeSummaryDto(
    decimal ValorTotalInvestido,
    decimal ValorAtualCarteira,
    decimal PlTotal,
    decimal RentabilidadePercentual);

public record AporteHistoricoDto(DateTime Data, decimal Valor, string Parcela);

public record EvolucaoCarteiraDto(DateTime Data, decimal ValorCarteira, decimal ValorInvestido, decimal Rentabilidade);
